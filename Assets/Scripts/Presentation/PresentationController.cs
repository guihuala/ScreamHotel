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
        public Transform guestsRoot;
        public RoomView roomPrefab;
        public PawnView ghostPrefab;
        public GuestView guestPrefab; 
        public Transform roofPrefab;
        
        [Header("Floor/Room Layout (relative to roomsRoot)")]
        [Tooltip("第1层相对 roomsRoot 的本地Y；第n层 = roomBaseY + (n-1)*floorSpacing。")]
        public float roomBaseY = 0f;

        [Tooltip("楼层间距（沿 Y 叠层）。")]
        public float floorSpacing = 8f;

        [Tooltip("左右两侧两间房的 X 偏移（电梯井位于 X=0；内侧靠近电梯，外侧更远）。")]
        public float xInner = 3.0f, xOuter = 6.0f;

        [Tooltip("电梯井预制体。若提供，每层在楼层中心生成一份。")]
        public Transform elevatorPrefab;
        
        private readonly HashSet<int> _elevatorsSpawned = new();
        
        [Header("Ghost Spawn Room (relative to ghostsRoot)")]
        [Tooltip("鬼出生‘独立房间’的根节点（必须设置）；建议这个root放在希望的独立房间中心。")]
        public Transform ghostSpawnRoomRoot;
        
        [Tooltip("所有鬼的固定 Z（XY 平面演出时建议=与独立房间所在平面一致）。")]
        public float spawnFixedZ = 0f;

        [Header("Guest Queue (relative to guestQueueRoot)")]
        [Tooltip("队列所在的房间/区域根节点（建议挂一个 BoxCollider 作为可视范围）。")]
        public Transform guestQueueRoot;

        [Tooltip("X 方向相邻客人的间距（世界单位）。")]
        public float queueSpacingX = 0.8f;

        [Tooltip("Y 方向换行间距（世界单位）。")]
        public float queueRowHeight = 0.7f;

        [Tooltip("每行最多多少人；<=0 表示自动根据 BoxCollider 宽度计算。")]
        public int queueWrapCount = 0;

        [Tooltip("队列的固定 Z（保持在 XY 平面）。")]
        public float queueFixedZ = 0f;


        private Transform currentRoof;  // 当前屋顶实例
        
        // 运行时映射
        private readonly Dictionary<string, RoomView> _roomViews = new();
        private readonly Dictionary<string, PawnView> _ghostViews = new();
        private readonly Dictionary<string, GuestView> _guestViews = new();

        // 缓存/复用的待命点
        private readonly Dictionary<string, Transform> _stagingByGhost = new();

        void OnEnable()
        {
            EventBus.Subscribe<GameStateChanged>(OnGameState);
            EventBus.Subscribe<RoomPurchasedEvent>(OnRoomPurchased);
            EventBus.Subscribe<RoomUpgradedEvent>(OnRoomUpgraded);
            EventBus.Subscribe<NightResolved>(OnNightResolved);
            EventBus.Subscribe<FloorBuiltEvent>(OnFloorBuilt);
            EventBus.Subscribe<RoofUpdateNeeded>(OnRoofUpdateNeeded);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChanged>(OnGameState);
            EventBus.Unsubscribe<RoomPurchasedEvent>(OnRoomPurchased);
            EventBus.Unsubscribe<RoomUpgradedEvent>(OnRoomUpgraded);
            EventBus.Unsubscribe<NightResolved>(OnNightResolved);
            EventBus.Unsubscribe<FloorBuiltEvent>(OnFloorBuilt);
            EventBus.Unsubscribe<RoofUpdateNeeded>(OnRoofUpdateNeeded);
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
                rv.transform.position = GetRoomSpawnPositionById(r.Id);
                rv.Bind(r);
                _roomViews[r.Id] = rv;
            }

            // 鬼怪
            foreach (var g in w.Ghosts)
            {
                if (_ghostViews.ContainsKey(g.Id)) continue;
                var pv = Instantiate(ghostPrefab, ghostsRoot);
                pv.BindGhost(g);

                // 随机出生在独立房间内
                pv.SnapTo(GetRandomGhostSpawnPos());

                _ghostViews[g.Id] = pv;
            }
            
            foreach (var g in w.Guests)
            {
                if (_guestViews.ContainsKey(g.Id)) continue;
                var gv = Instantiate(guestPrefab, guestsRoot);
                gv.BindGuest(g.Id);
                gv.SnapTo(GetGuestQueueWorldPos(_guestViews.Count));
                _guestViews[g.Id] = gv;
            }
            
            // 更新屋顶位置
            UpdateRoofPosition();
        }

        private void UpdateRoofPosition()
        {
            if (roofPrefab == null) return;

            // 获取当前最高层的 Y 坐标
            int highestFloor = GetHighestFloor();
            float topY = roomBaseY + highestFloor * floorSpacing;

            // 如果没有屋顶实例，则创建一个
            if (currentRoof == null)
            {
                currentRoof = Instantiate(roofPrefab, roomsRoot);
            }

            // 更新屋顶位置
            currentRoof.position = roomsRoot.TransformPoint(new Vector3(0f, topY, 0f));
        }
        
        private int GetHighestFloor()
        {
            int maxFloor = 0;
            foreach (var roomId in _roomViews.Keys)
            {
                if (TryParseRoomId(roomId, out int floor, out string slot))
                {
                    maxFloor = Mathf.Max(maxFloor, floor);
                }
            }
            return maxFloor;
        }
        
        private bool TryParseRoomId(string roomId, out int floor, out string slot)
        {
            floor = 0; slot = null;
            if (string.IsNullOrEmpty(roomId)) return false;
            var fIdx = roomId.IndexOf("_F", System.StringComparison.Ordinal);
            if (fIdx < 0) return false;
            var usIdx = roomId.IndexOf('_', fIdx + 2);
            if (usIdx < 0) return false;
            var num = roomId.Substring(fIdx + 2, usIdx - (fIdx + 2));
            if (!int.TryParse(num, out floor)) return false;
            slot = roomId.Substring(usIdx + 1); // LA/LB/RA/RB
            return true;
        }

        private Vector3 GetRoomSpawnPositionById(string roomId)
        {
            if (!TryParseRoomId(roomId, out var floor, out var slot))
            {
                return roomsRoot != null ? roomsRoot.position : Vector3.zero;
            }
            
            // 1) 本地楼层中心（相对 roomsRoot）
            float ly = roomBaseY + (floor - 1) * floorSpacing;
            var floorCenterLocal = new Vector3(0f, ly, 0f);

            // 2) 槽位决定 X 偏移
            float lx = slot switch
            {
                "LA" => -xInner, // 左内
                "LB" => -xOuter, // 左外
                "RA" =>  xInner, // 右内
                "RB" =>  xOuter, // 右外
                _    =>  0f,
            };

            var local = floorCenterLocal + new Vector3(lx, 0f, 0f);

            // 3) 楼层电梯（仅一次）
            TrySpawnElevatorOnce(floor, floorCenterLocal);

            // 4) 转世界坐标
            return roomsRoot ? roomsRoot.TransformPoint(local) : local;
        }
        
        private Vector3 GetGuestQueueWorldPos(int index)
        {
            if (guestQueueRoot == null)
            {
                // 没配就退回 ghostsRoot/guestsRoot 原点
                var origin = guestsRoot != null ? guestsRoot.position :
                    (ghostsRoot != null ? ghostsRoot.position : Vector3.zero);
                return new Vector3(origin.x + index * queueSpacingX, origin.y, queueFixedZ);
            }

            // 优先使用 BoxCollider 的范围来自动算可摆宽度
            var box = guestQueueRoot.GetComponentInChildren<BoxCollider>();
            if (box != null)
            {
                var size = box.size;
                var center = box.center;

                // 能摆的每行个数（自动或手动上限）
                int perRowAuto = Mathf.Max(1, Mathf.FloorToInt(size.x / Mathf.Max(0.01f, queueSpacingX)));
                int perRow = (queueWrapCount > 0) ? Mathf.Min(queueWrapCount, perRowAuto) : perRowAuto;

                int row = index / perRow;
                int col = index % perRow;

                float startXLocal = center.x - (perRow - 1) * 0.5f * queueSpacingX; // 居中分布
                float x = startXLocal + col * queueSpacingX;
                float y = center.y - row * queueRowHeight;

                var local = new Vector3(x, y, queueFixedZ);
                var world = box.transform.TransformPoint(local);
                world.z = queueFixedZ;
                return world;
            }
            else
            {
                int perRow = (queueWrapCount > 0) ? queueWrapCount : 8;
                int row = index / perRow;
                int col = index % perRow;

                float startXLocal = -(perRow - 1) * 0.5f * queueSpacingX;
                var local = new Vector3(startXLocal + col * queueSpacingX, -row * queueRowHeight, queueFixedZ);

                return guestQueueRoot.TransformPoint(local);
            }
        }

        private void TrySpawnElevatorOnce(int floor, Vector3 floorCenterLocal)
        {
            if (elevatorPrefab == null || _elevatorsSpawned.Contains(floor)) return;

            var t = Instantiate(elevatorPrefab, roomsRoot);
            t.position = roomsRoot ? roomsRoot.TransformPoint(floorCenterLocal) : floorCenterLocal;
            t.name = $"Elevator_F{floor}";
            _elevatorsSpawned.Add(floor);
        }
        
        // ---------- 布局计算 ----------
        private Vector3 GetRandomGhostSpawnPos()
        {
            if (ghostSpawnRoomRoot == null)
            {
                return ghostsRoot ? ghostsRoot.position : Vector3.zero;
            }
            
            var box = ghostSpawnRoomRoot.GetComponentInChildren<BoxCollider>();
            
            // 在 box 的本地范围内均匀采样
            var size = box.size;
            var center = box.center;
            float rx = Random.Range(-size.x * 0.5f, size.x * 0.5f);
            float ry = Random.Range(-size.y * 0.5f, size.y * 0.5f);
            var local = new Vector3(center.x + rx, center.y + ry, spawnFixedZ);
            
            var world = box.transform.TransformPoint(local);
            world.z = spawnFixedZ; // 强制 XY 平面
            return world;
        }

        // ---------- 事件响应 ----------
        void OnGameState(GameStateChanged e)
        {
            if (e.State is GameState.Day)
            {
                // Day：保证视图与数据一致
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
                
                // NightShow：把已分配的客人移动到房间“客人锚点”
                foreach (var room in w.Rooms)
                {
                    for (int i = 0; i < room.AssignedGuestIds.Count; i++)
                    {
                        var gid = room.AssignedGuestIds[i];
                        if (_guestViews.TryGetValue(gid, out var gv) && _roomViews.TryGetValue(room.Id, out var rv))
                        {
                            Transform anchor;
                            if (rv.TryGetGuestAnchor(i, out anchor))
                            {
                                gv.MoveTo(anchor, 0.5f);
                            }
                            else
                            {
                                // 兜底：没有客人锚点就用鬼的锚点或房间自身
                                anchor = rv.GetAnchor(i);
                                gv.MoveTo(anchor, 0.5f);
                            }
                        }
                    }
                }
            }
        }

        void OnRoomPurchased(RoomPurchasedEvent ev)
        {
            var r = game.World.Rooms.First(x => x.Id == ev.RoomId);
            var rv = Instantiate(roomPrefab, roomsRoot);
            rv.transform.position = GetRoomSpawnPositionById(r.Id);
            rv.Bind(r);
            _roomViews[r.Id] = rv;
        }
        
        void OnRoomUpgraded(RoomUpgradedEvent ev)
        {
            if (_roomViews.TryGetValue(ev.RoomId, out var rv))
            {
                var r = game.World.Rooms.First(x => x.Id == ev.RoomId);
                rv.Refresh(r);
            }
        }

        void OnNightResolved(NightResolved ev)
        {
            // 清场：独立房间随机点
            foreach (var kv in _ghostViews)
            {
                Vector3 randomPos = GetRandomGhostSpawnPos();
                Transform tempTransform = new GameObject().transform;
                tempTransform.position = randomPos;  // 使用随机位置
                kv.Value.MoveTo(tempTransform, 0.4f);  // 传递临时的 Transform
                Destroy(tempTransform.gameObject);  // 结束后销毁临时对象
            }
            
            // 客人按 World.Guests 的顺序回到队列
            var order = game.World.Guests.Select((g, i) => new { g.Id, Index = i }).ToList();
            foreach (var item in order)
            {
                if (_guestViews.TryGetValue(item.Id, out var gv))
                {
                    var tmp = new GameObject("tmpTarget").transform;
                    tmp.position = GetGuestQueueWorldPos(item.Index);
                    gv.MoveTo(tmp, 0.4f);
                    Destroy(tmp.gameObject);
                }
            }
        }
                
        void OnFloorBuilt(FloorBuiltEvent ev)
        {
            UpdateRoofPosition();
        }

        void OnRoofUpdateNeeded(RoofUpdateNeeded ev)
        {
            UpdateRoofPosition();
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
        
        private Vector3 GetStagingPosForIndex(int index)
        {
            // 在 ghostsRoot 附近按索引偏移
            float spacing = 2f;
            Vector3 basePos = ghostsRoot ? ghostsRoot.position : Vector3.zero;
            return basePos + new Vector3(index * spacing, 0, 0);
        }

        private int GetIndexForGhost(string ghostId)
        {
            // 尝试用当前字典顺序做一个稳定索引
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