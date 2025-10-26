using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.Systems;
using ScreamHotel.UI;
using UnityEngine.Serialization;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class RoomDropZone : MonoBehaviour, IDropZone
    {
        [Header("Build VFX")]
        [Tooltip("建造/升级时播放的粒子特效预制体")]
        public ParticleSystem buildFxPrefab;
        
        [Header("Build VFX")]
        public GameObject buildIcon;
        public GameObject nightIcon;

        
        private Game game;
        private RoomView _rv; // 只用于拿锚点/可视
        private string roomId;
        private Color _origColor;
        private GameObject currentBuildIcon; // 存储当前显示的建造图标

        // 系统
        private AssignmentSystem _assign => GetSystem<AssignmentSystem>(game, "_assignmentSystem");
        private BuildSystem _build    => GetSystem<BuildSystem>(game, "_buildSystem");
        private object _dayNightSysRaw=> GetSystem<object>(game, "_dayNightSystem");
        private HoverUIController _hoverUI;

        // 正在建造中的鬼，避免重复触发
        private readonly HashSet<string> _busyGhosts = new HashSet<string>();

        void Awake()
        {
            if (game == null) game = FindObjectOfType<Core.Game>();
            _rv = GetComponent<RoomView>();
            _hoverUI = FindObjectOfType<HoverUIController>(true);
            
            if (buildIcon != null) buildIcon.SetActive(false);
            if (nightIcon != null) nightIcon.SetActive(false);
        }

        public void SetRoomId(string newRoomId) => roomId = newRoomId;

        // 夜间/非建造分支使用；白天建造逻辑不走容量校验
        public bool CanAccept(string id, bool isGhost = true)
        {
            var w = game.World;
            var r = w.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null) return false;

            if (r.Level == 0)
            {
                return false;
            }
            
            if (isGhost)
            {
                var g = w.Ghosts.FirstOrDefault(x => x.Id == id);
                if (g == null || g.State is GhostState.Training) return false;
                if (r.AssignedGhostIds.Count >= r.Capacity) return false;
            }
            else
            {
                var guest = w.Guests.FirstOrDefault(x => x.Id == id);
                if (guest == null) return false;
                if (r.AssignedGuestIds.Count >= r.Capacity) return false;
            }
            return true;
        }

        public bool TryDrop(string id, bool isGhost, out Transform targetAnchor)
        {
            targetAnchor = null;

            var w = game.World;
            var r = w.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null)
            {
                Debug.LogWarning($"房间 {roomId} 未找到！");
                return false;
            }
            
            if (r.Level == 0)
            {
                return false;
            }
            
            if (isGhost && IsDaytime())
            {
                // 显示白天的建造图标
                if (buildIcon != null) buildIcon.SetActive(true);
                if (nightIcon != null) nightIcon.SetActive(false);
            }
            else
            {
                // 显示夜晚的放置图标
                if (buildIcon != null) buildIcon.SetActive(false);
                if (nightIcon != null) nightIcon.SetActive(true);
            }
            
            if (isGhost && IsDaytime())
            {
                if (_build == null)
                {
                    return false;
                }
                if (_busyGhosts.Contains(id))
                {
                    return false;
                }

                // 选择一个锚点让鬼“待在房间里”进行施工（不做数据上的分配）
                var anchor = (_rv != null && _rv.ghostAnchors != null && _rv.ghostAnchors.Length > 0)
                    ? _rv.ghostAnchors[0] : transform;
                targetAnchor = anchor;

                if (r.Level == 0)
                {
                    // Lv0 -> Lv1：无需选择恐惧属性
                    StartCoroutine(Co_BuildOrUpgrade(id, anchor, () =>
                    {
                        // 施工结束时再真正扣费/解锁
                        return _build.TryUnlockRoom(roomId);
                    }));
                    return true; // 让鬼进入锚点“施工”
                }
                else if (r.Level == 1)
                {
                    // Lv1 -> Lv2：先选择恐惧属性，然后开始施工
                    if (_hoverUI == null)
                    {
                        return false;
                    }

                    var attach = _rv != null ? _rv.transform : transform;
                    _hoverUI.OpenPickFearPanel(id, attach, 0, (ghostId, tag, slotIdx) =>
                    {
                        // 选完后再让鬼开始“施工 1~2s”，结束时真正升级
                        StartCoroutine(Co_BuildOrUpgrade(ghostId, anchor, () =>
                        {
                            return _build.TryUpgradeRoom(roomId, tag); // Lv1->Lv2 必须有 tag
                        }));
                    });
                    
                    return true;
                }
                else if (r.Level == 2)
                {
                    // Lv2 -> Lv3：直接施工，不更新“按属性”的外观
                    StartCoroutine(Co_BuildOrUpgrade(id, anchor, () =>
                    {
                        return _build.TryUpgradeRoom(roomId); // 不改变 tag
                    }));
                    return true;
                }
                else
                {
                    Debug.Log("房间已满级，无法继续升级。");
                    return false;
                }
            }

            // ============ 夜晚或非鬼：恢复“分配”逻辑 ============
            if (!CanAccept(id, isGhost))
            {
                return false;
            }

            if (isGhost)
            {
                if (_assign != null && _assign.TryAssignGhostToRoom(id, roomId))
                {
                    var index = r.AssignedGhostIds.IndexOf(id);
                    targetAnchor = _rv != null ? _rv.GetAnchor(index) : transform;
                    return true;
                }
            }
            else
            {
                if (_assign != null && _assign.TryAssignGuestToRoom(id, roomId))
                {
                    var idx = Mathf.Max(0, r.AssignedGuestIds.IndexOf(id));
                    targetAnchor = _rv != null ? _rv.GetGuestAnchor(idx) : transform;
                    return true;
                }
            }
            
            return false;
        }
        
        private IEnumerator Co_BuildOrUpgrade(string ghostId, Transform anchor, System.Func<bool> buildAction)
        {
            _busyGhosts.Add(ghostId);

            AudioManager.Instance.PlaySfx("Build");
            
            // 播放粒子
            ParticleSystem fx = null;
            if (buildFxPrefab != null)
            {
                fx = Instantiate(buildFxPrefab, anchor.position, Quaternion.identity);
                fx.Play();
            }
            
            float duration = Random.Range(1f, 1.5f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                yield return null;
            }

            bool ok = false;
            try
            {
                ok = buildAction != null && buildAction.Invoke();
            }
            finally
            {
                // 停止并销毁粒子
                if (fx != null)
                {
                    fx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    Destroy(fx.gameObject, 0.05f);
                }
                _busyGhosts.Remove(ghostId);
            }
            
            if (!ok) Debug.LogWarning("建造/升级失败：可能是金币不足或规则限制。");
        }
        
        public void ShowHoverFeedback(string id, bool isGhost)
        {
            var w = game.World;
            var r = w.Rooms.FirstOrDefault(x => x.Id == roomId);
            
            if (r == null || r.Level == 0)
            {
                if (nightIcon) nightIcon.SetActive(false);
                return;
            }
            
            bool showDayBuild = isGhost && IsDaytime();
            if (buildIcon)  buildIcon.SetActive(showDayBuild);
            if (nightIcon)  nightIcon.SetActive(!showDayBuild);
        }


        public void ClearFeedback()
        {
            if (buildIcon)      buildIcon.SetActive(false);
            if (nightIcon) nightIcon.SetActive(false);
        }

        private static T GetSystem<T>(object obj, string field) where T : class
        {
            var f = obj.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f?.GetValue(obj) as T;
        }

        private bool IsDaytime()
        {
            if (game?.TimeSystem == null) return true;
            return game.TimeSystem.GetCurrentTimePeriod() == GameState.Day;
        }
    }
}
