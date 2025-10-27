using System.Collections;
using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Data;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    public class NightShowDirector : MonoBehaviour
    {
        private Vector3 _cameraOriginalPos;
        private float _cameraOriginalFov;
        private bool _cameraOriginalSaved = false;

        [Header("镜头控制参数（基准值）")]
        [Tooltip("相机聚焦平移速度（越大越快）。基准=4，基于它计算聚焦用时 1/speed")]
        public float focusMoveSpeed = 4f;          // 基准：聚焦移动速度（秒^-1），用时≈1/speed
        [Tooltip("聚焦后的小停留（基准值），会按预算等比缩放")]
        public float focusStayTime = 0.6f;         // 基准：每房停留
        [Tooltip("所有房间演出完后再延迟一点点进入结算")]
        public float afterShowDelay = 0.8f;        // 结尾额外停顿（不参与等比缩放，避免为0）

        [Header("FOV（透视相机）")]
        public float focusFov = 45f;               // 聚焦时FOV
        public float focusFovLerpSpeed = 3f;       // FOV 拉近速度系数（会按预算缩放）
        public float restoreFovSpeed = 2f;         // FOV 还原速度系数（会按预算缩放）

        [Header("每位客人动画等待（基准值）")]
        [Tooltip("播放每位客人的shock/idle后，等待的基准时长（用于让演出能看清），会按预算缩放")]
        public float perGuestWait = 1.5f;          // 基准：每位客人的等待
        
        [Header("成功粒子")]
        public GameObject successParticlePrefab;   // 命中成功时实例化

        // —— 下限（防止被压得看不见）——
        private const float MIN_MOVE_TIME = 0.22f;     // 每次镜头聚焦的最短用时
        private const float MIN_GUEST_WAIT = 0.55f;    // 每位客人最短等待
        private const float MIN_STAY_TIME = 0.30f;     // 聚焦后最短停留

        // 精度阈值
        private const float PosEps = 0.05f;
        private const float FovEps = 0.05f;

        // —— 计算得出的“本轮动态参数”（根据预算缩放）——
        private float _moveSpeedScaled;
        private float _perGuestWaitScaled;
        private float _stayTimeScaled;
        private float _focusFovLerpScaled;
        private float _restoreFovLerpScaled;

        public IEnumerator PlayNightShow(ExecNightResolved result)
        {
            var game = FindObjectOfType<Game>();
            var camController = FindObjectOfType<CameraController>();
            var cam = camController != null ? camController.GetComponent<Camera>() : Camera.main;
            
            // 禁用玩家相机操控
            if (camController != null) camController.SetPlayerControl(false);

            if (cam != null && !_cameraOriginalSaved)
            {
                _cameraOriginalPos = cam.transform.position;
                _cameraOriginalFov = cam.fieldOfView;
                _cameraOriginalSaved = true;
            }

            // 只选“有客人的房间”
            var filteredRooms = result.RoomResults
                .Where(rr => rr.GuestResults != null && rr.GuestResults.Count > 0)
                .ToList();

            // —— 依据“夜间执行阶段”预算，计算缩放参数 ——
            PlanDurationsByBudget(game, filteredRooms);

            // 串场（中途不回正）
            foreach (var roomResult in filteredRooms)
            {
                var roomView = FindObjectsOfType<RoomView>().FirstOrDefault(r => r.roomId == roomResult.RoomId);
                if (roomView == null) continue;

                // 1) 先聚焦房间，并等待“对准完成”
                if (camController != null)
                    yield return StartCoroutine(FocusOnRoom(camController, roomView, cam));

                // 2) 对准后播放本房的每位客人
                foreach (var gRes in roomResult.GuestResults)
                {
                    bool success = gRes.Hits >= 1;
                    PlayRoomAnimation(roomView, gRes, success);

                    if (success) SpawnSuccessVfxAtGuest(roomView, gRes.GuestId);
                    if (success && camController != null)
                        camController.StartCoroutine(ShakeOnce(camController, 0.3f, 0.25f));

                    yield return new WaitForSeconds(_perGuestWaitScaled);
                }

                yield return new WaitForSeconds(_stayTimeScaled);
                
                ResetRoomToIdle(roomView, roomResult);
            }

            // —— 所有房间结束：回正（位置+FOV） ——
            if (camController != null)
                yield return StartCoroutine(RestoreCamera(camController, cam));

            yield return new WaitForSeconds(afterShowDelay);

            // 恢复玩家操控
            if (camController != null) camController.SetPlayerControl(true);

            game.StartSettlement();
        }

        /// <summary>
        /// 按“夜间执行阶段”的总预算，计算一次本轮应使用的时长/速度。
        /// 预算来源：TimeSystem.dayDurationInSeconds * nightExecuteRatio（规则可从 World.Config.Rules 读到）。
        /// </summary>
        private void PlanDurationsByBudget(Game game, System.Collections.Generic.List<RoomNightResult> rooms)
        {
            // 1) 计算夜间执行阶段预算（秒）
            float daySeconds = game?.TimeSystem?.dayDurationInSeconds ?? 300f;
            var rules = game?.World?.Config?.Rules;
            float rDay = 0.50f, rShow = 0.20f, rExec = 0.20f, rSettle = 0.10f;
            if (rules != null)
            {
                rDay    = Mathf.Max(0f, rules.dayRatio);
                rShow   = Mathf.Max(0f, rules.nightShowRatio);
                rExec   = Mathf.Max(0f, rules.nightExecuteRatio);
                rSettle = Mathf.Max(0f, rules.settlementRatio);
                float sum = rDay + rShow + rExec + rSettle;
                if (sum > 0.0001f) { rDay/=sum; rShow/=sum; rExec/=sum; rSettle/=sum; }
                else { rDay=0.50f; rShow=0.20f; rExec=0.20f; rSettle=0.10f; }
            }
            float execBudget = Mathf.Max(0.1f, daySeconds * rExec);

            // 2) 估算“基准情况下”总耗时（不缩放）
            float moveTimeBase = 1f / Mathf.Max(0.0001f, focusMoveSpeed);
            int totalGuests = rooms.Sum(r => r.GuestResults != null ? r.GuestResults.Count : 0);
            int roomCount = rooms.Count;

            float plannedTotal =
                roomCount * moveTimeBase +
                totalGuests * perGuestWait +
                roomCount * focusStayTime;

            // 3) 计算缩放系数（<=1 则压缩；>1 则按基准跑）
            float scale = (plannedTotal > 0.0001f) ? Mathf.Min(1f, execBudget / plannedTotal) : 1f;

            // 4) 应用缩放，并与下限取max
            float moveTimeScaled = Mathf.Max(MIN_MOVE_TIME, moveTimeBase * scale);
            _perGuestWaitScaled  = Mathf.Max(MIN_GUEST_WAIT, perGuestWait * scale);
            _stayTimeScaled      = Mathf.Max(MIN_STAY_TIME, focusStayTime * scale);

            // 让“速度”与“用时”一致：moveTime = 1 / speed → speed = 1 / moveTime
            _moveSpeedScaled     = 1f / moveTimeScaled;

            // FOV 的lerp速度也跟着等比缩放（保持视觉节奏）
            _focusFovLerpScaled  = Mathf.Max(0.1f, focusFovLerpSpeed * scale);
            _restoreFovLerpScaled= Mathf.Max(0.1f, restoreFovSpeed * scale);
        }

        private void PlayRoomAnimation(RoomView room, GuestNightResult gRes, bool success)
        {
            var guestView = FindObjectsOfType<GuestView>().FirstOrDefault(g => g.guestId == gRes.GuestId);
            if (guestView == null) return;

            // 获取游戏世界和数据库
            var game = FindObjectOfType<Game>();
            var dataManager = FindObjectOfType<DataManager>();
    
            // 通过 TypeId 查找对应的 GuestTypeConfig
            GuestTypeConfig guestTypeConfig = null;
            if (game?.World != null && dataManager?.Database != null)
            {
                // 从世界中找到对应的客人
                var guest = game.World.Guests.FirstOrDefault(g => g.Id == gRes.GuestId);
                if (guest != null && !string.IsNullOrEmpty(guest.TypeId))
                {
                    // 从数据库获取客人类型配置
                    dataManager.Database.GuestTypes.TryGetValue(guest.TypeId, out guestTypeConfig);
                }
            }

            // 找出房间内所有鬼怪（按世界分配）
            var ghosts = FindObjectsOfType<PawnView>().Where(p => GetRoomOfGhost(p) == room.roomId).ToList();

            foreach (var g in ghosts)
            {
                var spine = g.GetComponentInChildren<Spine.Unity.SkeletonAnimation>();
                if (spine != null)
                    spine.state.SetAnimation(0, "shock", false);
            }

            var guestSpine = guestView.GetComponentInChildren<Spine.Unity.SkeletonAnimation>();
            if (guestSpine != null)
            {
                guestSpine.state.SetAnimation(0, success ? "shock" : "idle", false);
            }
            
            if (success)
            {
                if (guestTypeConfig?.successAudio != null)
                {
                    AudioManager.Instance.PlaySfx(guestTypeConfig.successAudio);
                }
            }
            else
            {
                if (guestTypeConfig?.failureAudio != null)
                {
                    AudioManager.Instance.PlaySfx(guestTypeConfig.failureAudio);
                }
            }
        }

        private string GetRoomOfGhost(PawnView ghost)
        {
            var world = FindObjectOfType<Game>()?.World;
            if (world == null) return null;
            return world.Rooms.FirstOrDefault(r => r.AssignedGhostIds.Contains(ghost.ghostId))?.Id;
        }

        // —— 相机聚焦（位置+FOV），时长由 _moveSpeedScaled 决定：用时≈1/_moveSpeedScaled ——
        private IEnumerator FocusOnRoom(CameraController camCtrl, RoomView room, Camera cam)
        {
            if (camCtrl == null || room == null || cam == null) yield break;

            Vector3 startPos = cam.transform.position;
            Vector3 targetPos = room.transform.position;
            targetPos.z = startPos.z;

            float startFov = cam.fieldOfView;
            float t = 0;

            while (t < 1f)
            {
                t += Time.deltaTime * _moveSpeedScaled;                      // 使用动态速度
                float eased = Mathf.SmoothStep(0, 1, t);
                cam.transform.position = Vector3.Lerp(startPos, targetPos, eased);
                cam.fieldOfView = Mathf.Lerp(startFov, focusFov, eased * _focusFovLerpScaled);
                yield return null;
            }

            cam.transform.position = targetPos;
            cam.fieldOfView = focusFov;

            // 等待到“足够对准”
            while ((cam.transform.position - targetPos).sqrMagnitude > (PosEps * PosEps) ||
                   Mathf.Abs(cam.fieldOfView - focusFov) > FovEps)
            {
                yield return null;
            }
        }

        // —— 回正（位置+FOV），用 _moveSpeedScaled、_restoreFovLerpScaled 保持节奏一致 ——
        private IEnumerator RestoreCamera(CameraController camCtrl, Camera cam)
        {
            if (!_cameraOriginalSaved || cam == null) yield break;

            Vector3 startPos = cam.transform.position;
            Vector3 targetPos = _cameraOriginalPos;
            float startFov = cam.fieldOfView;
            float t = 0;

            while (t < 1f)
            {
                t += Time.deltaTime * _moveSpeedScaled;
                float eased = Mathf.SmoothStep(0, 1, t);
                cam.transform.position = Vector3.Lerp(startPos, targetPos, eased);
                cam.fieldOfView = Mathf.Lerp(startFov, _cameraOriginalFov, eased * _restoreFovLerpScaled);
                yield return null;
            }

            cam.transform.position = targetPos;
            cam.fieldOfView = _cameraOriginalFov;
        }

        // —— 单次轻微震动（命中时） ——
        private IEnumerator ShakeOnce(CameraController camCtrl, float duration, float strength)
        {
            var camTransform = camCtrl.transform;
            Vector3 basePos = camTransform.localPosition;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                float s = Mathf.Lerp(strength, 0, t);
                float ox = Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f;
                float oy = Mathf.PerlinNoise(0f, Time.time * 30f) - 0.5f;
                camTransform.localPosition = basePos + new Vector3(ox, oy, 0) * s;
                yield return null;
            }

            camTransform.localPosition = basePos;
        }

        // —— 在对应客人“锚点/位置”生成粒子（优先用 RoomView 的锚点） ——
        private void SpawnSuccessVfxAtGuest(RoomView roomView, string guestId)
        {
            if (successParticlePrefab == null) return;

            var guestView = FindObjectsOfType<GuestView>().FirstOrDefault(g => g.guestId == guestId);
            if (guestView == null) return;

            Transform spawnAt = guestView.transform;
            var world = FindObjectOfType<Game>()?.World;
            if (world != null)
            {
                var room = world.Rooms.FirstOrDefault(r => r.Id == roomView.roomId);
                if (room != null && room.AssignedGuestIds != null)
                {
                    int idx = room.AssignedGuestIds.IndexOf(guestId);
                    if (idx >= 0 && roomView.TryGetGuestAnchor(idx, out var anchor) && anchor != null)
                        spawnAt = anchor; // 直接使用房间的客人锚点（你的 RoomView 已提供该API）
                }
            }

            var go = Instantiate(successParticlePrefab, spawnAt.position, Quaternion.identity);
            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(go, ps.main.duration + ps.main.startLifetime.constantMax + 0.5f);
            }
            else
            {
                Destroy(go, 2f);
            }
        }
        
        private void ResetRoomToIdle(RoomView roomView, RoomNightResult roomResult)
        {
            // 鬼怪：按分配在本房的 PawnView
            var ghosts = FindObjectsOfType<PawnView>().Where(p => GetRoomOfGhost(p) == roomView.roomId);
            foreach (var g in ghosts)
            {
                var spine = g.GetComponentInChildren<Spine.Unity.SkeletonAnimation>();
                if (spine != null)
                    spine.state.SetAnimation(0, "idle", true);
            }

            // 客人：本房结果中的所有 GuestId
            foreach (var gRes in roomResult.GuestResults)
            {
                var guestView = FindObjectsOfType<GuestView>().FirstOrDefault(v => v.guestId == gRes.GuestId);
                if (guestView == null) continue;
                var guestSpine = guestView.GetComponentInChildren<Spine.Unity.SkeletonAnimation>();
                if (guestSpine != null)
                    guestSpine.state.SetAnimation(0, "idle", true);
            }
        }
    }
}
