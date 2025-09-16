using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Core;

namespace ScreamHotel.Systems
{
    public class BuildSystem
    {
        private readonly World _world;
        public BuildSystem(World world) { _world = world; }

        // 价格档，目前简单取第一个 RoomPriceConfig
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

        // 升级房间：Lv1->Lv2（可选设置标签）、Lv2->Lv3（容量提高，保留标签）
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
            else if (r.Level == 2)
            {
                if (_world.Economy.Gold < cfg.upgradeToLv3) return false;
                _world.Economy.Gold -= cfg.upgradeToLv3;

                r.Level = 3;
                r.Capacity = cfg.capacityLv3; // Lv3 扩容
                // 保留 Lv2 已设置的房间标签

                EventBus.Raise(new GoldChanged(_world.Economy.Gold));
                EventBus.Raise(new RoomUpgradedEvent(r.Id, r.Level));
                return true;
            }

            return false; // 已经是 Lv3
        }

        // （保留，按需扩展）夜晚后结算钩子
        public void ApplySettlement(NightResults results)
        {
            // 可在此处理建造队列完成、维修折旧等
        }
    }

    // ===== 白天准备（强制休息递减、刷新当天资源）=====
    public class DayPhaseSystem
    {
        private readonly World _world;
        public DayPhaseSystem(World world) { _world = world; }

        public void PrepareDay(int day)
        {
            // 强制休息递减
            foreach (var g in _world.Ghosts)
            {
                if (g.DaysForcedRest > 0)
                {
                    g.DaysForcedRest--;
                    g.State = (g.DaysForcedRest == 0) ? GhostState.Idle : GhostState.Resting;
                }
            }

            // TODO：生成当日客源池 / 候选鬼怪 / 商店刷新等
        }
    }

    // ===== 进程推进（难度/解锁曲线）=====
    public class ProgressionSystem
    {
        private readonly World _world;
        public ProgressionSystem(World world) { _world = world; }

        public void Advance(NightResults results, int dayIndex)
        {
            // TODO：根据 ProgressionConfig 调整可用恐惧池、解锁特殊客人/功能、切换价格档等
        }
    }
}
