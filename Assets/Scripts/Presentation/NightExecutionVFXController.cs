using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.Presentation;

namespace ScreamHotel.Systems
{
    /// <summary>
    /// 夜晚执行阶段的演出控制器（回合化推进版本）
    /// - 结算层仍沿用 NightExecutionSystem 的一次性胜负/收益判定；
    /// - 此控制器把“瞬时胜负”展开为多个回合（sweep），在每轮结束时推进“惊吓累计”，
    ///   确保旧逻辑命中≥1的配置在若干轮后必定达标（兼容旧胜负）。
    /// </summary>
    public class NightExecutionVFXController : MonoBehaviour
    {
        #region 回合推进所需的临时状态

        private class GuestBattleState
        {
            public float Acc;      // 当前累计惊吓值
            public float Required; // 达成阈值（BarMax * RequiredPercent）
            public int   Hits;     // 当晚命中数（来自 ExecNightResolved）
        }

        // 每位客人的回合状态
        private Dictionary<string, GuestBattleState> _guestStates;

        #endregion

        [System.Serializable]
        public class TagVfx
        {
            public FearTag tag;
            public ParticleSystem vfx;
        }

        [Header("VFX Prefabs (fallback)")]
        public ParticleSystem defaultHitVfx;
        public ParticleSystem defaultMissVfx;

        [Header("Per-Tag VFX (override)")]
        [Tooltip("为不同的恐惧Tag配置不同粒子；如果命中多个Tag，会依次播放多个VFX")]
        public List<TagVfx> tagVfxOverrides = new();

        [Header("Motion Settings")]
        [Tooltip("单次“上前+后退”的节拍（秒）")]
        public float approachBeat = 0.6f;
        [Tooltip("鬼怪上前的位移")]
        public float ghostStep = 0.6f;
        [Tooltip("客人后仰/抖动幅度")]
        public float guestNudge = 0.35f;

        [Header("Room Detection")]
        [Tooltip("当鬼与本房任意客人的距离 < 该阈值时，认为该鬼属于本房间（米）")]
        public float ghostProximity = 4.0f;

        [Header("Desync Settings")]
        [Tooltip("同一房内的随机启动延时占比（相对房间预算）；0.4 表示最长延时= roomBudget * 0.4")]
        public float startDelayRoomMax = 0.4f;
        [Tooltip("单个实体的节拍抖动百分比（0.15 => ±15%）")]
        [Range(0f, 0.5f)] public float beatJitterPct = 0.15f;
        [Tooltip("鬼上前位移的随机抖动幅度（米)")]
        public float stepJitter = 0.15f;
        [Tooltip("客人后仰幅度的随机抖动幅度（米)")]
        public float nudgeJitter = 0.1f;
        [Tooltip("位置噪声（Perlin）振幅（米）")]
        public float noiseWobbleAmp = 0.03f;
        [Tooltip("位置噪声（Perlin）频率（越大摆动越快）")]
        public float noiseWobbleFreq = 1.7f;

        [Header("Loop Settings")]
        [Tooltip("单次循环占用夜间执行总时长的比例。0.35 => 一轮大概用掉 35% 的时长，然后继续下一轮")]
        [Range(0.05f, 1f)] public float sweepShareOfExec = 0.35f;

        [Header("回合数（整晚）")]
        [SerializeField, Min(1)]
        private int roundsPerNight = 3; // 策划可调：整晚计划跑几轮（建议 2~4）
        private int _rounds = 1;        // 运行时实际使用的回合数

        private Coroutine _loopRoutine;
        private Game _game;
        private Dictionary<FearTag, ParticleSystem> _tag2Vfx;

        void Awake()
        {
            _game = FindObjectOfType<Game>();

            // tag->vfx 映射
            _tag2Vfx = tagVfxOverrides
                .Where(m => m != null && m.vfx != null)
                .GroupBy(m => m.tag)
                .ToDictionary(g => g.Key, g => g.First().vfx);

            EventBus.Subscribe<ExecNightResolved>(OnNightResolved);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ExecNightResolved>(OnNightResolved);
            if (_loopRoutine != null) StopCoroutine(_loopRoutine);
        }

        /// <summary>
        /// 夜晚一次性结算结果（命中/收益）到达：初始化回合状态并启动循环演出
        /// </summary>
        private void OnNightResolved(ExecNightResolved r)
        {
            if (_game == null || _game.State != GameState.NightExecute) return;

            // 1) 初始化每位客人的“回合状态”
            _guestStates = new Dictionary<string, GuestBattleState>();
            var guestDict = _game.World.Guests.ToDictionary(g => g.Id, g => g); // 含 BarMax/RequiredPercent
            foreach (var room in r.RoomResults)
            {
                foreach (var gRes in room.GuestResults)
                {
                    if (!guestDict.TryGetValue(gRes.GuestId, out var g)) continue;
                    float required = Mathf.Max(0.0001f, g.BarMax * g.RequiredPercent); // 防 0
                    _guestStates[gRes.GuestId] = new GuestBattleState
                    {
                        Acc = 0f, Required = required, Hits = gRes.Hits
                    };
                }
            }

            // 2) 确定本晚回合数（优先使用 roundsPerNight；也可改为基于 sweepShareOfExec 的推导）
            _rounds = Mathf.Max(1, roundsPerNight);
            // 也可使用下面这一行自动推导（取整晚约 1 / 占比 的轮数）：
            // _rounds = Mathf.Max(1, Mathf.RoundToInt(1f / Mathf.Clamp(sweepShareOfExec, 0.05f, 1f)));

            // 3) 启动循环演出
            if (_loopRoutine != null) StopCoroutine(_loopRoutine);
            _loopRoutine = StartCoroutine(NightExecLoop(r));
        }

        private IEnumerator NightExecLoop(ExecNightResolved result)
        {
            bool firstLoop = true;
            while (_game != null && _game.State == GameState.NightExecute)
            {
                // 每一轮播放一遍“所有房间”的动作（=一个回合）
                yield return StartCoroutine(PlayOneSweep(result, firstLoop));
                firstLoop = false;

                // 节奏留缝
                yield return null;
            }

            _loopRoutine = null;
        }

        /// <summary>
        /// 播放一轮（所有房间各执行一次预算内的演出），作为“一个回合”
        /// </summary>
        private IEnumerator PlayOneSweep(ExecNightResolved result, bool spawnVfxThisLoop)
        {
            if (_game == null) yield break;

            float execDuration = CalcExecPhaseDurationSeconds(_game);
            float beat = Mathf.Max(0.1f, approachBeat);

            // 一轮 ≈ 总时长 * sweepShareOfExec
            int roomCount = Mathf.Max(1, result.RoomResults.Count);
            float roomBudget = Mathf.Max(beat, execDuration * Mathf.Clamp01(sweepShareOfExec) / roomCount);

            // 缓存视图（如需优化，可在 OnNightResolved 缓一次并在客人/鬼视图变化时刷新）
            var guestViews = FindObjectsOfType<GuestView>().ToDictionary(g => g.guestId, g => g);
            var allGhostViews = FindObjectsOfType<PawnView>();

            foreach (var room in result.RoomResults)
            {
                // 本房的客人与鬼
                var roomGuestViews = new List<GuestView>();
                foreach (var gRes in room.GuestResults)
                    if (guestViews.TryGetValue(gRes.GuestId, out var gv))
                        roomGuestViews.Add(gv);

                var roomGhosts = SelectGhostsNearGuests(allGhostViews, roomGuestViews, ghostProximity);

                var coroutines = new List<Coroutine>();
                float maxDelay = roomBudget * Mathf.Clamp01(startDelayRoomMax);

                // 客人反应 +（首轮可选）粒子
                foreach (var gRes in room.GuestResults)
                {
                    if (!guestViews.TryGetValue(gRes.GuestId, out var gv)) continue;

                    bool isHit = gRes.Hits > 0;
                    int dayIndex = _game != null ? _game.DayIndex : 0;
                    int guestKey = !string.IsNullOrEmpty(gRes.GuestId) ? StableHash(gRes.GuestId) : gv.GetInstanceID();
                    int seed = guestKey ^ dayIndex;

                    float delay = maxDelay * Hash01(seed, 1);
                    float u = Hash01(seed, 2);

                    coroutines.Add(StartCoroutine(
                        PlayGuestReact(gv.transform, isHit, roomBudget, gRes.Hits, delay, u)
                    ));

                    if (spawnVfxThisLoop)
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

                // 鬼的上前-后退
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

                // 房间级预算：在该房预算时间内循环等待
                float waited = 0f;
                while (_game != null && _game.State == GameState.NightExecute && waited < roomBudget)
                {
                    waited += Time.deltaTime;
                    yield return null;
                }

                if (_game == null || _game.State != GameState.NightExecute) yield break;
            }

            // ★ 每轮结束：推进所有相关客人的“惊吓进度”（回合制核心）
            foreach (var room in result.RoomResults)
            {
                foreach (var gRes in room.GuestResults)
                {
                    if (_guestStates == null || !_guestStates.TryGetValue(gRes.GuestId, out var st)) continue;

                    if (st.Hits > 0 && st.Acc < st.Required)
                    {
                        // 确保 Hits>=1 在 _rounds 轮后必达阈值；Hits>1 可稍快（但不影响结算数值）
                        float perTurn = (st.Required / Mathf.Max(1, _rounds)) * Mathf.Clamp(st.Hits, 1, 3);
                        st.Acc = Mathf.Min(st.Required, st.Acc + perTurn);

                        // 如需 UI 条/气泡：计算进度百分比 pct = st.Acc / st.Required 并分发给 GuestView
                        // float pct = st.Acc / st.Required;
                        // guestViews.TryGetValue(gRes.GuestId, out var gv);
                        // gv?.SetScareProgress(pct);
                    }
                }
            }
        }

        /// <summary>
        /// 选择靠近本房客人的鬼（近似地归属到房间）
        /// </summary>
        private IEnumerable<PawnView> SelectGhostsNearGuests(IEnumerable<PawnView> allGhosts, List<GuestView> guests, float proximity)
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

                float wob = (Mathf.PerlinNoise((Time.time + 3.1f) * noiseWobbleFreq, u + 0.37f) - 0.5f) * 2f * noiseWobbleAmp;
                t.position = Vector3.Lerp(origin + (Vector3.back + Vector3.up * 0.2f) * amp, origin, k)
                             + Vector3.right * wob * (isHit ? 1f : 0.4f);
                yield return null;
            }

            t.position = origin;
        }

        /// <summary>
        /// 鬼：上前一步再退回
        /// </summary>
        private IEnumerator PlayGhostStep(Transform t, float budget, float startDelay, float u)
        {
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            Vector3 origin = t.position;

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
                float wob = (Mathf.PerlinNoise((Time.time + 2.3f) * noiseWobbleFreq, u + 0.89f) - 0.5f) * 2f * noiseWobbleAmp;

                t.position = Vector3.Lerp(origin + Vector3.forward * localStep, origin, k)
                             + Vector3.right * wob;
                yield return null;
            }

            t.position = origin;
        }

        /// <summary>
        /// 从 TimeSystem 规则换算夜间执行时长（秒）
        /// </summary>
        private float CalcExecPhaseDurationSeconds(Game game)
        {
            float daySeconds = Mathf.Max(1f, game.TimeSystem.dayDurationInSeconds);

            var rules = game.World?.Config?.Rules;
            float rDay = 0.5f, rShow = 0.2f, rExec = 0.2f, rSettle = 0.1f;
            if (rules != null)
            {
                rDay   = Mathf.Max(0f, rules.dayRatio);
                rShow  = Mathf.Max(0f, rules.nightShowRatio);
                rExec  = Mathf.Max(0f, rules.nightExecuteRatio);
                rSettle= Mathf.Max(0f, rules.settlementRatio);
                float sum = rDay + rShow + rExec + rSettle;
                if (sum > 1e-4f)
                {
                    rDay   /= sum;
                    rShow  /= sum;
                    rExec  /= sum;
                    rSettle/= sum;
                }
                else
                {
                    rDay = 0.5f; rShow = 0.2f; rExec = 0.2f; rSettle = 0.1f;
                }
            }

            return Mathf.Max(0.25f, daySeconds * rExec);
        }

        #region 工具函数

        // 稳定 [0,1) 随机；用两个整数键（比如 guestId/ghostId + “今天第几天”）保证可重复
        private float Hash01(int a, int b = 0)
        {
            unchecked
            {
                uint x = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9E3779B9u;
                x ^= x << 13; x ^= x >> 17; x ^= x << 5;
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
                const uint FNV_PRIME  = 16777619;
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
