using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using ScreamHotel.Core;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    public class PresentationController : MonoBehaviour
    {
        [Header("Refs")]
        public Game game;
        public Transform roomsRoot;
        public Transform ghostsRoot;
        public RoomView roomPrefab;
        public PawnView ghostPrefab;

        // ---------- 房间摆放配置 ----------
        [Header("Room Layout (优先使用显式插槽)")]
        [Tooltip("若提供，则按顺序使用这些 Transform 作为房间生成位置。")]
        public List<Transform> roomSlots = new();

        [Tooltip("若 roomSlots 为空，则启用网格排布。")]
        public bool useGridWhenNoSlots = true;

        [Tooltip("网格起点（世界坐标）。也可把其设为 roomsRoot 的子节点以便整体移动。")]
        public Vector3 roomGridOrigin = new Vector3(0, 0, 0);

        [Tooltip("网格列数（每行房间数量）。")]
        public int roomGridColumns = 4;

        [Tooltip("网格间距（X / Z）。")]
        public Vector2 roomGridSpacing = new Vector2(10f, 10f);

        [Tooltip("房间放置的 Y 高度。")]
        public float roomY = 0f;

        [Header("是否使用 roomsRoot 的局部空间计算网格")]
        public bool roomGridInLocalSpace = false;

        // ---------- 待命区摆放配置 ----------
        [Header("Staging Layout (优先使用显式待命点)")]
        [Tooltip("若提供，则按顺序使用这些 Transform 作为鬼怪待命位置。")]
        public List<Transform> ghostStagingSlots = new();

        [Tooltip("若 stagingSlots 为空，则按 起点+步进 生成线性待命位。")]
        public bool useLinearStagingWhenNoSlots = true;

        [Tooltip("待命区起点（世界坐标）。")]
        public Vector3 stagingOrigin = new Vector3(-5f, 0f, 0f);

        [Tooltip("每个待命位的步进向量（世界坐标）。例如(-2.5, 0, 0)表示沿X轴每个向左2.5单位。")]
        public Vector3 stagingStep = new Vector3(-2.5f, 0f, 0f);

        [Tooltip("待命位的固定 Z（如果你只想在 XY 面上展示，请把这个 Z 设为你的展示平面Z）。")]
        public float stagingFixedZ = 0f;

        // 运行时映射
        private readonly Dictionary<string, RoomView> _roomViews = new();
        private readonly Dictionary<string, PawnView> _ghostViews = new();

        // 缓存/复用的待命点（按 ghostId）
        private readonly Dictionary<string, Transform> _stagingByGhost = new();

        void OnEnable()
        {
            EventBus.Subscribe<GameStateChanged>(OnGameState);
            EventBus.Subscribe<RoomPurchasedEvent>(OnRoomPurchased);
            EventBus.Subscribe<RoomUpgradedEvent>(OnRoomUpgraded);
            EventBus.Subscribe<NightResolved>(OnNightResolved);
        }
        void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChanged>(OnGameState);
            EventBus.Unsubscribe<RoomPurchasedEvent>(OnRoomPurchased);
            EventBus.Unsubscribe<RoomUpgradedEvent>(OnRoomUpgraded);
            EventBus.Unsubscribe<NightResolved>(OnNightResolved);
        }

        void Start()
        {
            if (game == null) game = FindObjectOfType<Game>();
            BuildInitialViews();
        }

        // ---------- 构建 & 刷新 ----------
        void BuildInitialViews()
        {
            var w = game.World;
            if (w == null) return;

            // 房间
            foreach (var r in w.Rooms.OrderBy(x => x.Id))
            {
                if (_roomViews.ContainsKey(r.Id)) continue;
                var rv = Instantiate(roomPrefab, roomsRoot);
                rv.transform.position = GetRoomSpawnPosition(_roomViews.Count);
                rv.Bind(r);
                _roomViews[r.Id] = rv;
            }

            // 鬼怪
            foreach (var g in w.Ghosts)
            {
                if (_ghostViews.ContainsKey(g.Id)) continue;
                var pv = Instantiate(ghostPrefab, ghostsRoot);
                pv.BindGhost(g);
                var stagingT = GetStagingTransform(g.Id);
                pv.SnapTo(stagingT.position); // 待命位
                _ghostViews[g.Id] = pv;

                // 如果有拖拽组件，绑定 ghostId 和 game
                var drag = pv.GetComponent<DraggablePawn>();
                if (drag)
                {
                    drag.ghostId = g.Id;
                    drag.game = game;
                }
            }
        }

        // ---------- 布局计算 ----------
        Vector3 GetRoomSpawnPosition(int index)
        {
            // 1) 显式插槽优先
            if (roomSlots != null && index < roomSlots.Count && roomSlots[index] != null)
                return roomSlots[index].position;

            // 2) 网格
            if (useGridWhenNoSlots)
            {
                int cols = Mathf.Max(1, roomGridColumns);
                float sx = roomGridSpacing.x;
                float sz = roomGridSpacing.y;
                int x = index % cols;
                int z = index / cols;

                Vector3 pos = new Vector3(
                    roomGridOrigin.x + x * sx,
                    roomY,
                    roomGridOrigin.z + z * sz
                );

                if (roomGridInLocalSpace && roomsRoot != null)
                    pos = roomsRoot.TransformPoint(pos);

                return pos;
            }

            // 3) 兜底：放在 roomsRoot 原点
            return roomsRoot != null ? roomsRoot.position : new Vector3(0, roomY, 0);
        }

        Vector3 GetStagingPosForIndex(int index)
        {
            Vector3 pos;

            // 1) 显式待命插槽
            if (ghostStagingSlots != null && index < ghostStagingSlots.Count && ghostStagingSlots[index] != null)
            {
                pos = ghostStagingSlots[index].position;
            }
            else
            {
                // 2) 线性起点+步进
                if (useLinearStagingWhenNoSlots)
                {
                    pos = stagingOrigin + stagingStep * index;
                }
                else
                {
                    pos = stagingOrigin;
                }
            }

            // 固定 Z（便于只在 XY 面展示）
            pos.z = stagingFixedZ;
            return pos;
        }

        // ---------- 事件响应 ----------
        void OnGameState(GameStateChanged e)
        {
            if (e.State is GameState.Day)
            {
                // Day：保证视图与数据一致（房间数量/升级后的容量等）
                SyncAll();
            }
            else if (e.State is GameState.NightShow)
            {
                // NightShow：把已分配的鬼移动到房间锚点
                var w = game.World;
                foreach (var room in w.Rooms)
                {
                    for (int i = 0; i < room.AssignedGhostIds.Count; i++)
                    {
                        var gid = room.AssignedGhostIds[i];
                        if (_ghostViews.TryGetValue(gid, out var pawn) && _roomViews.TryGetValue(room.Id, out var rv))
                        {
                            var anchor = rv.GetAnchor(i);
                            pawn.MoveTo(anchor, 0.5f);
                        }
                    }
                }
            }
        }

        void OnRoomPurchased(RoomPurchasedEvent ev)
        {
            var r = game.World.Rooms.First(x => x.Id == ev.RoomId);
            var rv = Instantiate(roomPrefab, roomsRoot);
            rv.transform.position = GetRoomSpawnPosition(_roomViews.Count);
            rv.Bind(r);
            _roomViews[r.Id] = rv;
        }

        void OnRoomUpgraded(RoomUpgradedEvent ev)
        {
            if (_roomViews.TryGetValue(ev.RoomId, out var rv))
            {
                var r = game.World.Rooms.First(x => x.Id == ev.RoomId);
                rv.Refresh(r);
                rv.PulseSuccess();
            }
        }

        void OnNightResolved(NightResolved ev)
        {
            var res = (NightResults)ev.Results;
            foreach (var rr in res.RoomDetails)
            {
                if (_roomViews.TryGetValue(rr.RoomId, out var rv))
                {
                    if (rr.Counter) rv.PulseCounter();
                    else if (rr.TotalScare >= rr.Required) rv.PulseSuccess();
                    else rv.PulseFail();
                }
            }

            // 清场：把鬼回到各自待命位（不再创建临时对象）
            int idx = 0;
            foreach (var kv in _ghostViews)
            {
                var t = GetStagingTransform(kv.Key, idx);
                kv.Value.MoveTo(t, 0.4f);
                idx++;
            }
        }

        void SyncAll()
        {
            BuildInitialViews();
            foreach (var r in game.World.Rooms)
                if (_roomViews.TryGetValue(r.Id, out var rv)) rv.Refresh(r);
        }

        // ---------- 待命位查询 ----------
        public Transform GetStagingTransform(string ghostId) => GetStagingTransform(ghostId, GetIndexForGhost(ghostId));
        public Transform GetStagingTransform(string ghostId, int suggestedIndex)
        {
            if (_stagingByGhost.TryGetValue(ghostId, out var t) && t != null) return t;

            t = new GameObject($"Staging_{ghostId}").transform;
            var pos = GetStagingPosForIndex(suggestedIndex);
            t.position = pos;
            _stagingByGhost[ghostId] = t;
            return t;
        }

        private int GetIndexForGhost(string ghostId)
        {
            // 尝试用当前字典顺序做一个稳定索引；也可以根据 World.Ghosts 顺序来决定
            int i = 0;
            foreach (var id in game.World.Ghosts.Select(g => g.Id))
            {
                if (id == ghostId) return i;
                i++;
            }
            // 若未找到，退回已有 staging 数量
            return _stagingByGhost.Count;
        }
    }
}
