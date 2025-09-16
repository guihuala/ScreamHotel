using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using ScreamHotel.Domain;
using ScreamHotel.Core;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    public class PresentationController : MonoBehaviour
    {
        [Header("Refs")]
        public ScreamHotel.Core.Game game;
        public Transform roomsRoot;        // 指向场景中的 RoomsRoot
        public Transform ghostsRoot;       // 可空；自动创建
        public RoomView roomPrefab;        // 绑定你的 RoomSlot 预制
        public PawnView ghostPrefab;       // 绑定你的 GhostPawn 预制

        // 运行时映射
        private readonly Dictionary<string, RoomView> _roomViews = new();
        private readonly Dictionary<string, PawnView> _ghostViews = new();

        void Awake()
        {
            if (ghostsRoot == null) { var go = new GameObject("GhostsRoot"); ghostsRoot = go.transform; }
        }

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
            if (game == null) game = FindObjectOfType<ScreamHotel.Core.Game>();
            BuildInitialViews();
        }

        // ---------- 构建 & 刷新 ----------
        void BuildInitialViews()
        {
            var w = game.World;
            if (w == null) return;

            // 房间
            foreach (var r in w.Rooms.OrderBy(x=>x.Id))
            {
                if (_roomViews.ContainsKey(r.Id)) continue;
                var rv = Instantiate(roomPrefab, roomsRoot);
                rv.transform.position = NextRoomPosition(_roomViews.Count);
                rv.Bind(r);
                _roomViews[r.Id] = rv;
            }

            // 鬼怪
            foreach (var g in w.Ghosts)
            {
                if (_ghostViews.ContainsKey(g.Id)) continue;
                var pv = Instantiate(ghostPrefab, ghostsRoot);
                pv.BindGhost(g);
                pv.SnapTo(GetStagingPosFor(g.Id)); // 待命位
                _ghostViews[g.Id] = pv;
            }
        }

        Vector3 NextRoomPosition(int index)
        {
            int cols = 4;
            float spacing = 2.5f;
            int x = index % cols;
            int z = index / cols;
            return new Vector3(x * spacing, 0, z * spacing);
        }

        Vector3 GetStagingPosFor(string ghostId)
        {
            // 待命位：放在左侧一列
            int i = _ghostViews.Count;
            return new Vector3(-2.5f, 0.55f, 1.2f * i);
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
            rv.transform.position = NextRoomPosition(_roomViews.Count);
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

            // 清场：把鬼回到待命位
            foreach (var kv in _ghostViews)
                kv.Value.MoveTo(new GameObject().transform, 0.01f); // 先停掉移动
            int idx = 0;
            foreach (var kv in _ghostViews)
            {
                var target = new GameObject("tmp").transform; // 临时目标避免闭包问题
                target.position = new Vector3(-2.5f, 0.55f, 1.2f * idx++);
                kv.Value.MoveTo(target, 0.4f);
                Destroy(target.gameObject, 0.5f);
            }
        }

        void SyncAll()
        {
            // 新增房间/鬼时补视图；升级后刷新标签
            BuildInitialViews();
            foreach (var r in game.World.Rooms)
                if (_roomViews.TryGetValue(r.Id, out var rv)) rv.Refresh(r);
        }
    }
}
