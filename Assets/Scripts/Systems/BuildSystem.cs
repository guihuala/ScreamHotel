using System;
using System.Collections.Generic;
using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Core;
using ScreamHotel.Presentation;
using UnityEngine;

namespace ScreamHotel.Systems
{
    /// <summary>楼层里的四个房间槽位：左A/左B/右A/右B（中间电梯不占槽位）。</summary>
    public enum RoomSlot { LA, LB, RA, RB }
    
    public class BuildSystem
    {
        private readonly World _world;
        public BuildSystem(World world) { _world = world; }
        
        // 楼层建造基础费用与增量
        private int floorBaseCost = 100;
        private int floorStepCost = 50;

        // 获取价格，目前区第一个价格
        private Data.RoomPriceConfig CurrentPrice()
        {
            return _world.Config.RoomPrices.Values.First();
        }

        // 购买新房（Lv1）
        public bool TryBuyRoom(out string newRoomId)
        {
            var cfg = CurrentPrice();
            newRoomId = null;

            if (_world.Economy.Gold < cfg.buyCost) return false;

            _world.Economy.Gold -= cfg.buyCost;

            var id = $"Room_{_world.Rooms.Count + 1:00}";
            var room = new Room
            {
                Id = id,
                Level = 1,
                Capacity = cfg.capacityLv1,
                RoomTag = null
            };
            _world.Rooms.Add(room);
            newRoomId = id;

            EventBus.Raise(new GoldChanged(_world.Economy.Gold));
            EventBus.Raise(new RoomPurchasedEvent(id));
            return true;
        }

        // 升级房间：Lv1->Lv2、Lv2->Lv3
        public bool TryUpgradeRoom(string roomId, FearTag? setTagOnLv2 = null)
        {
            var r = _world.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null) return false;

            var cfg = CurrentPrice();

            if (r.Level == 1)
            {
                if (_world.Economy.Gold < cfg.upgradeToLv2) return false;
                _world.Economy.Gold -= cfg.upgradeToLv2;

                r.Level = 2;
                r.Capacity = cfg.capacityLv1; // 如需 Lv2 改容量，可改为独立字段
                if (cfg.lv2HasTag && setTagOnLv2.HasValue) r.RoomTag = setTagOnLv2.Value;

                EventBus.Raise(new GoldChanged(_world.Economy.Gold));
                EventBus.Raise(new RoomUpgradedEvent(r.Id, r.Level));
                return true;
            }
            if (r.Level == 2)
            {
                if (_world.Economy.Gold < cfg.upgradeToLv3) return false;
                _world.Economy.Gold -= cfg.upgradeToLv3;

                r.Level = 3;
                r.Capacity = cfg.capacityLv3; // 扩容

                EventBus.Raise(new GoldChanged(_world.Economy.Gold));
                EventBus.Raise(new RoomUpgradedEvent(r.Id, r.Level));
                return true;
            }

            return false; // 已经是 Lv3
        }
        
        public void ApplySettlement(NightResults results)
        {
            // 可在此处理建造队列完成、维修折旧等
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

        /// <summary>计算“第 floor 层”的建造费用</summary>
        public int GetFloorBuildCost(int floor)
        {
            floor = Math.Max(1, floor);
            return floorBaseCost + (floor - 1) * floorStepCost;
        }

        /// <summary>列出某层的房间Id（按 LA,LB,RA,RB 顺序）</summary>
        public IEnumerable<string> EnumerateRoomIdsOnFloor(int floor)
        {
            yield return MakeRoomId(floor, RoomSlot.LA);
            yield return MakeRoomId(floor, RoomSlot.LB);
            yield return MakeRoomId(floor, RoomSlot.RA);
            yield return MakeRoomId(floor, RoomSlot.RB);
        }

        #endregion
        
        // ====== 内部实现 ======
        private bool HasAnyFloor() => _world.Rooms.Count > 0;

        private void CreateFloorInternally(int floor, bool free)
        {
            // 若该层已存在任一房间则忽略
            if (_world.Rooms.Any(r => TryParseFloor(r.Id, out var f) && f == floor)) return;

            var cfg = CurrentPrice();
            foreach (var slot in new[] { RoomSlot.LA, RoomSlot.LB, RoomSlot.RA, RoomSlot.RB })
            {
                var id = MakeRoomId(floor, slot);
                var room = new Room
                {
                    Id = id,
                    Level = 1,
                    Capacity = cfg.capacityLv1,
                    RoomTag = null
                };
                _world.Rooms.Add(room);
                EventBus.Raise(new RoomPurchasedEvent(id)); // “解锁/生成”也用买房事件，便于 UI 同步
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
