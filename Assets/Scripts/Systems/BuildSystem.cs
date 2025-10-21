using System;
using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Core;

namespace ScreamHotel.Systems
{
    /// <summary>楼层里的四个房间槽位：左A/左B/右A/右B（中间电梯不占槽位）。</summary>
    public enum RoomSlot { LA, LB, RA, RB }
    
    public class BuildSystem
    {
        private readonly World _world;
        public BuildSystem(World world) { _world = world; }

        public bool TryUnlockRoom(string roomId)
        {
            var r = _world.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null) return false;
            var rules = _world.Config?.Rules;
            if (rules == null) return false;

            // 仅 Lv0（锁定）可解锁
            if (r.Level != 0) return false;

            if (_world.Economy.Gold < rules.roomUnlockCost) return false;
            _world.Economy.Gold -= rules.roomUnlockCost;

            r.Level = 1;                      // Lv0 → Lv1
            r.Capacity = rules.capacityLv1;
            r.RoomTag = null;

            EventBus.Raise(new GoldChanged(_world.Economy.Gold));
            EventBus.Raise(new RoomUnlockedEvent(r.Id));   // 新事件
            EventBus.Raise(new RoomUpgradedEvent(r.Id, r.Level)); // 通知刷新
            return true;
        }

        // 升级房间：Lv1->Lv2、Lv2->Lv3
        public bool TryUpgradeRoom(string roomId, FearTag? setTagOnLv2 = null)
        {
            var r = _world.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null) return false;
            var rules = _world.Config?.Rules;
            if (r.Level == 0) return false; // ← 必须先解锁
            if (rules == null) return false;

            if (r.Level == 1)
            {
                // 若 Lv2 需要恐惧标签但未提供，则不允许升级
                if (rules.lv2HasTag && !setTagOnLv2.HasValue)
                    return false; // 必须先选择恐惧属性

                if (_world.Economy.Gold < GetRoomUpgradeCost(roomId, r.Level)) return false;
                _world.Economy.Gold -= GetRoomUpgradeCost(roomId, r.Level);

                r.Level = 2;
                r.Capacity = rules.capacityLv1; // 若Lv2容量不同，可再加字段 capacityLv2
                if (rules.lv2HasTag && setTagOnLv2.HasValue) r.RoomTag = setTagOnLv2.Value;

                EventBus.Raise(new GoldChanged(_world.Economy.Gold));
                EventBus.Raise(new RoomUpgradedEvent(r.Id, r.Level));
                return true;
            }
            if (r.Level == 2)
            {
                // Lv2 -> Lv3
                if (_world.Economy.Gold < GetRoomUpgradeCost(roomId, r.Level)) return false;
                _world.Economy.Gold -= GetRoomUpgradeCost(roomId, r.Level);

                r.Level = 3;
                r.Capacity = rules.capacityLv3; // 扩容
    
                EventBus.Raise(new GoldChanged(_world.Economy.Gold));
                EventBus.Raise(new RoomUpgradedEvent(r.Id, r.Level));
                return true;
            }
            return false; // 已经Lv3
        }
        
        #region 对外Api

        public bool TryBuildNextFloor(out int newFloor)
        {
            newFloor = GetNextFloorIndex();
            var cost = GetFloorBuildCost(newFloor);
            if (_world.Economy.Gold < cost) return false;

            _world.Economy.Gold -= cost;
            EventBus.Raise(new GoldChanged(_world.Economy.Gold));

            CreateFloorInternally(newFloor, free: true);
            EventBus.Raise(new FloorBuiltEvent(newFloor));

            // 发送屋顶更新事件
            EventBus.Raise(new RoofUpdateNeeded());

            return true;
        }

        /// <summary>返回当前最高楼层（不存在则0）。</summary>
        public int GetHighestFloor()
        {
            int max = 0;
            foreach (var r in _world.Rooms)
            {
                if (TryParseFloor(r.Id, out var f))
                    max = Math.Max(max, f);
            }
            return max;
        }

        /// <summary>获取下一层索引（最高层+1；若无房则返回1）</summary>
        public int GetNextFloorIndex()
        {
            var h = GetHighestFloor();
            return Math.Max(1, h + 1);
        }
        
        public int GetFloorBuildCost(int newFloor)
        {
            var rules = _world.Config?.Rules;
            if (rules == null) return 0;
            double cost = rules.floorBuildBaseCost * Math.Pow(rules.floorCostGrowth, Math.Max(0, newFloor - 1));
            return (int)Math.Round(cost);
        }

        private int GetRoomUpgradeCost(string roomId, int currentLevel)
        {
            var rules = _world.Config?.Rules;
            if (rules == null || rules.roomUpgradeCosts == null) return 0;
            
            int idx = currentLevel - 1;
            if (idx < 0 || idx >= rules.roomUpgradeCosts.Length) return 0;
            return rules.roomUpgradeCosts[idx];
        }
        
        #endregion
        
        // ====== 内部实现 ======
        
        private void CreateFloorInternally(int floor, bool free)
        {
            if (_world.Rooms.Any(r => TryParseFloor(r.Id, out var f) && f == floor)) return;

            var rules = _world.Config?.Rules;
            var capLv1 = rules != null ? rules.capacityLv1 : 1;

            foreach (var slot in new[] { RoomSlot.LA, RoomSlot.LB, RoomSlot.RA, RoomSlot.RB })
            {
                var id = MakeRoomId(floor, slot);
                var room = new Room
                {
                    Id = id,
                    Level = 0,            // 原来是 1，改为 0：锁定态
                    Capacity = capLv1,    // 提前准备好容量
                    RoomTag = null
                };
                _world.Rooms.Add(room);
                EventBus.Raise(new RoomPurchasedEvent(id)); // 沿用
            }
        }

        private string MakeRoomId(int floor, RoomSlot slot) => $"Room_F{floor}_{slot}";
        private bool TryParseFloor(string roomId, out int floor)
        {
            floor = 0;
            // 形如 Room_F3_LA
            if (string.IsNullOrEmpty(roomId)) return false;
            var fIdx = roomId.IndexOf("_F", StringComparison.Ordinal);
            if (fIdx < 0) return false;
            var underscore = roomId.IndexOf('_', fIdx + 2);
            if (underscore < 0) return false;
            var num = roomId.Substring(fIdx + 2, underscore - (fIdx + 2));
            return int.TryParse(num, out floor);
        }
    }
}
