using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.Presentation;

namespace ScreamHotel.Systems
{
    public class NightExecutionVFXController : MonoBehaviour
    {
        [System.Serializable]
        public class TagVfx
        {
            public FearTag tag;
            public ParticleSystem vfx;
        }

        [Header("VFX Prefabs (fallback)")] public ParticleSystem defaultHitVfx;
        public ParticleSystem defaultMissVfx;

        [Header("Per-Tag VFX (override)")] [Tooltip("为不同的恐惧Tag配置不同粒子；如果命中多个Tag，会依次播放多个VFX")]
        public List<TagVfx> tagVfxOverrides = new();

        [Header("Motion Settings")] [Tooltip("单次“上前+后退”的节拍（秒）")]
        public float approachBeat = 0.6f;

        [Tooltip("鬼怪上前的位移")] public float ghostStep = 0.6f;
        [Tooltip("客人后仰/抖动幅度")] public float guestNudge = 0.35f;

        [Header("Room Detection")] [Tooltip("当鬼与本房任意客人的距离 < 该阈值时，认为该鬼属于本房间（米）")]
        public float ghostProximity = 4.0f;

        [Header("Desync Settings")] [Tooltip("同一房内的随机启动延时占比（相对房间预算）；0.4 表示最长延时= roomBudget * 0.4")]
        public float startDelayRoomMax = 0.4f;

        [Tooltip("单个实体的节拍抖动百分比（0.15 => ±15%）")] [Range(0f, 0.5f)]
        public float beatJitterPct = 0.15f;

        [Tooltip("鬼上前位移的随机抖动幅度（米）")] public float stepJitter = 0.15f;
        [Tooltip("客人后仰幅度的随机抖动幅度（米）")] public float nudgeJitter = 0.1f;
        [Tooltip("位置噪声（Perlin）振幅（米）")] public float noiseWobbleAmp = 0.03f;
        [Tooltip("位置噪声（Perlin）频率（越大摆动越快）")] public float noiseWobbleFreq = 1.7f;

        [Header("Loop Settings")] [Tooltip("单次循环占用夜间执行总时长的比例。0.35 => 一轮大概用掉 35% 的时长，然后继续下一轮")] [Range(0.05f, 1f)]
        public float sweepShareOfExec = 0.35f;

        [Tooltip("是否只在第一轮播放命中粒子，后续循环仅做位移动作避免刷屏")]
        public bool vfxOnlyFirstLoop = true;

        private Coroutine _loopRoutine;
        private Game _game;
        private Dictionary<FearTag, ParticleSystem> _tag2Vfx;

        void Awake()
        {
            _game = FindObjectOfType<Game>();
            _tag2Vfx = tagVfxOverrides
                .Where(m => m != null && m.vfx != null)
                .GroupBy(m => m.tag).ToDictionary(g => g.Key, g => g.First().vfx);

            EventBus.Subscribe<NightResolved>(OnNightResolved);
        }
        
        private void OnDestroy()
        {
            EventBus.Unsubscribe<NightResolved>(OnNightResolved);
            if (_loopRoutine != null) StopCoroutine(_loopRoutine);
        }

        private void OnNightResolved(NightResolved r)
        {
            if (_game == null || _game.State != GameState.NightExecute) return;

            // 如果上一次的循环还在，先停掉
            if (_loopRoutine != null) StopCoroutine(_loopRoutine);
            // 开启整段循环：会一直播到离开 NightExecute
            _loopRoutine = StartCoroutine(NightExecLoop(r));
        }

        private IEnumerator NightExecLoop(NightResolved result)
        {
            bool firstLoop = true;
            while (_game != null && _game.State == GameState.NightExecute)
            {
                // 每一轮播放一遍“所有房间”的动作
                yield return StartCoroutine(PlayOneSweep(result, firstLoop));
                firstLoop = false;

                // 小让步，避免过于紧凑
                yield return null;
            }

            _loopRoutine = null;
        }

        // 用于“单轮播放”的方法
        private IEnumerator PlayOneSweep(NightResolved result, bool spawnVfxThisLoop)
        {
            if (_game == null) yield break;

            Debug.Log("PlayOneSweep");
            float execDuration = CalcExecPhaseDurationSeconds(_game);
            float beat = Mathf.Max(0.1f, approachBeat);

            // 目标：一轮 ≈ 总时长 * sweepShareOfExec
            int roomCount = Mathf.Max(1, result.RoomResults.Count);
            float roomBudget = Mathf.Max(beat, execDuration * Mathf.Clamp01(sweepShareOfExec) / roomCount);

            // 缓存视图
            var guestViews = FindObjectsOfType<GuestView>().ToDictionary(g => g.guestId, g => g);
            var allGhostViews = FindObjectsOfType<PawnView>();

            foreach (var room in result.RoomResults)
            {
                // 房内客人与鬼
                var roomGuestViews = new List<GuestView>();
                foreach (var gRes in room.GuestResults)
                    if (guestViews.TryGetValue(gRes.GuestId, out var gv))
                        roomGuestViews.Add(gv);

                var roomGhosts = SelectGhostsNearGuests(allGhostViews, roomGuestViews, ghostProximity);

                var coroutines = new List<Coroutine>();
                float maxDelay = roomBudget * Mathf.Clamp01(startDelayRoomMax);

                // 客人反应 +（可选）粒子
                foreach (var gRes in room.GuestResults)
                {
                    if (!guestViews.TryGetValue(gRes.GuestId, out var gv)) continue;

                    bool isHit = gRes.Hits > 0;
                    int dayIndex = _game != null ? _game.DayIndex : 0;
                    int guestKey = !string.IsNullOrEmpty(gRes.GuestId)
                        ? StableHash(gRes.GuestId)
                        : gv.GetInstanceID();
                    int seed = guestKey ^ dayIndex;

                    float delay = maxDelay * Hash01(seed, 1);
                    float u = Hash01(seed, 2);

                    // —— 修复：去掉重复的 PlayGuestReact ——（你文件里这行被误复制了一次）
                    coroutines.Add(StartCoroutine(
                        PlayGuestReact(gv.transform, isHit, roomBudget, gRes.Hits, delay, u)
                    ));

                    if (spawnVfxThisLoop) // 只在本轮播粒子（后续轮避免刷屏）
                    {
                        if (isHit && gRes.EffectiveTags != null && gRes.EffectiveTags.Count > 0)
                        {
                            foreach (var tag in gRes.EffectiveTags)
                            {
                                if (_tag2Vfx.TryGetValue(tag, out var vfxPrefab) && vfxPrefab != null)
                                    SpawnAndAutoDestroy(vfxPrefab, gv.transform.position + Vector3.up * 1.2f);
                                else if (defaultHitVfx != null)
                                    SpawnAndAutoDestroy(defaultHitVfx, gv.transform.position + Vector3.up * 1.2f);
                            }
                        }
                        else if (!isHit && defaultMissVfx != null)
                        {
                            SpawnAndAutoDestroy(defaultMissVfx, gv.transform.position + Vector3.up * 1.0f);
                        }
                    }
                }

                // 本房的鬼上前-后退
                foreach (var pv in roomGhosts)
                {
                    int dayIndex = _game != null ? _game.DayIndex : 0;
                    int ghostKey = !string.IsNullOrEmpty(pv.ghostId) ? StableHash(pv.ghostId) : pv.GetInstanceID();
                    int seed = ghostKey ^ dayIndex;

                    float delay = maxDelay * Hash01(seed, 11);
                    float u = Hash01(seed, 12);
                    coroutines.Add(StartCoroutine(
                        PlayGhostStep(pv.transform, roomBudget, delay, u)
                    ));
                }

                // 房间级节奏控制：等待到本房预算用完
                float waited = 0f;
                while (_game != null && _game.State == GameState.NightExecute && waited < roomBudget)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                // 若阶段已切走（结算/白天等），立即结束循环
                if (_game == null || _game.State != GameState.NightExecute) yield break;
            }
        }

        private IEnumerable<PawnView> SelectGhostsNearGuests(IEnumerable<PawnView> allGhosts, List<GuestView> guests,
            float proximity)
        {
            if (guests == null || guests.Count == 0) return Enumerable.Empty<PawnView>();
            float p2 = proximity * proximity;
            var set = new HashSet<PawnView>();
            foreach (var pv in allGhosts)
            {
                Vector3 gp = pv.transform.position;
                foreach (var gv in guests)
                {
                    if (gv == null) continue;
                    if ((gv.transform.position - gp).sqrMagnitude <= p2)
                    {
                        set.Add(pv);
                        break;
                    }
                }
            }

            return set;
        }

        private void SpawnAndAutoDestroy(ParticleSystem prefab, Vector3 pos)
        {
            var vfx = Instantiate(prefab, pos, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + 0.3f);
        }

        private IEnumerator PlayGuestReact(Transform t, bool isHit, float budget, int hits, float startDelay, float u)
        {
            // 启动延时（去同频关键）
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            Vector3 origin = t.position;

            // 节拍抖动（±beatJitterPct）
            float localBeat = Jitter(approachBeat, beatJitterPct, u);
            float dur = Mathf.Min(budget * 0.6f, localBeat);

            // 幅度抖动
            float ampBase = (isHit ? 1f + 0.15f * Mathf.Clamp(hits - 1, 0, 3) : 0.5f) * guestNudge;
            float amp = ampBase + (u * 2f - 1f) * nudgeJitter;

            float half = Mathf.Max(0.12f, dur * 0.5f);

            // 上半段：后仰/后退
            float t0 = 0;
            while (t0 < 1f)
            {
                t0 += Time.deltaTime / half;
                float k = Mathf.SmoothStep(0, 1, t0);

                // 噪声（Perlin）——不同相位
                float wob = (Mathf.PerlinNoise(Time.time * noiseWobbleFreq, u) - 0.5f) * 2f * noiseWobbleAmp;

                t.position = origin
                             + (Vector3.back + Vector3.up * 0.2f) * (k * amp)
                             + Vector3.right * wob;
                yield return null;
            }

            // 下半段：回位 + 轻微摆动
            float t1 = 0;
            while (t1 < 1f)
            {
                t1 += Time.deltaTime / half;
                float k = Mathf.SmoothStep(0, 1, t1);

                float wob = (Mathf.PerlinNoise((Time.time + 3.1f) * noiseWobbleFreq, u + 0.37f) - 0.5f) * 2f *
                            noiseWobbleAmp;
                t.position = Vector3.Lerp(origin + (Vector3.back + Vector3.up * 0.2f) * amp, origin, k)
                             + Vector3.right * wob * (isHit ? 1f : 0.4f);
                yield return null;
            }

            t.position = origin;
        }

        // === 鬼怪：上前一步再退回 ===
        private IEnumerator PlayGhostStep(Transform t, float budget, float startDelay, float u)
        {
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            Vector3 origin = t.position;

            // 节拍抖动 + 步长抖动
            float localBeat = Jitter(approachBeat, beatJitterPct, u);
            float localStep = ghostStep + (u * 2f - 1f) * stepJitter;
            float dur = Mathf.Min(budget * 0.4f, localBeat);
            float half = Mathf.Max(0.12f, dur * 0.5f);

            float t0 = 0;
            while (t0 < 1f)
            {
                t0 += Time.deltaTime / half;
                float k = Mathf.SmoothStep(0, 1, t0);
                float wob = (Mathf.PerlinNoise(Time.time * noiseWobbleFreq, u + 0.12f) - 0.5f) * 2f * noiseWobbleAmp;

                t.position = origin
                             + Vector3.forward * (k * localStep)
                             + Vector3.right * wob;
                yield return null;
            }

            float t1 = 0;
            while (t1 < 1f)
            {
                t1 += Time.deltaTime / half;
                float k = Mathf.SmoothStep(0, 1, t1);
                float wob = (Mathf.PerlinNoise((Time.time + 2.3f) * noiseWobbleFreq, u + 0.89f) - 0.5f) * 2f *
                            noiseWobbleAmp;

                t.position = Vector3.Lerp(origin + Vector3.forward * localStep, origin, k)
                             + Vector3.right * wob;
                yield return null;
            }

            t.position = origin;
        }

        // 读取TimeSystem规则，得到夜间执行时长（秒）
        private float CalcExecPhaseDurationSeconds(Game game)
        {
            float daySeconds = Mathf.Max(1f, game.TimeSystem.dayDurationInSeconds);

            var rules = game.World?.Config?.Rules;
            float rDay = 0.5f, rShow = 0.2f, rExec = 0.2f, rSettle = 0.1f;
            if (rules != null)
            {
                rDay = Mathf.Max(0f, rules.dayRatio);
                rShow = Mathf.Max(0f, rules.nightShowRatio);
                rExec = Mathf.Max(0f, rules.nightExecuteRatio);
                rSettle = Mathf.Max(0f, rules.settlementRatio);
                float sum = rDay + rShow + rExec + rSettle;
                if (sum > 1e-4f)
                {
                    rDay /= sum;
                    rShow /= sum;
                    rExec /= sum;
                    rSettle /= sum;
                }
                else
                {
                    rDay = 0.5f;
                    rShow = 0.2f;
                    rExec = 0.2f;
                    rSettle = 0.1f;
                }
            }

            return Mathf.Max(0.25f, daySeconds * rExec);
        }

        #region 工具函数

        // 稳定[0,1) 随机；用两个整数键（比如 guestId / ghostId + “今天第几天”）保证可重复
        private float Hash01(int a, int b = 0)
        {
            unchecked
            {
                uint x = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9E3779B9u;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                return (x & 0x00FFFFFFu) / 16777216f; // 24bit
            }
        }

        private float Jitter(float baseVal, float pct, float u01)
        {
            float r = (u01 * 2f - 1f) * pct; // [-pct, +pct]
            return baseVal * (1f + r);
        }

        // 把字符串稳定地转成 int（FNV-1a 32bit）
        private static int StableHash(string s)
        {
            unchecked
            {
                const uint FNV_OFFSET = 2166136261;
                const uint FNV_PRIME = 16777619;
                uint h = FNV_OFFSET;
                if (!string.IsNullOrEmpty(s))
                {
                    for (int i = 0; i < s.Length; i++)
                    {
                        h ^= s[i];
                        h *= FNV_PRIME;
                    }
                }

                return (int)h;
            }
        }

        #endregion
    }
}