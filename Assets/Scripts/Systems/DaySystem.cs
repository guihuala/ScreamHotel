using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Core;
using UnityEngine;

namespace ScreamHotel.Systems
{
    // ===== 白天准备（刷新当天资源）=====
    public class DayPhaseSystem
    {
        private readonly World _world;
        private readonly DayGuestSpawner _guestSpawner;
        private readonly GhostShopSystem _ghostShop;

        public DayPhaseSystem(World world)
        {
            _world = world;
            _guestSpawner = new DayGuestSpawner(world);
            _ghostShop    = new GhostShopSystem(world);
        }
        
        public void PrepareDay(int dayIndex)
        {
            // 1) 清当日分配/状态
            foreach (var r in _world.Rooms) r.AssignedGuestIds.Clear();
            foreach (var g in _world.Ghosts)
            {
                if (g.State == GhostState.Working) g.State = GhostState.Idle;
                if (g.DaysForcedRest > 0) g.DaysForcedRest--;
                if (g.State == GhostState.Injured && g.DaysForcedRest <= 0) g.State = GhostState.Idle;
            }

            // 2) 刷新客人池：先清掉上一日的客人，再生成今日客源
            _world.Guests.Clear();

            int guestCountToday = (_world.Config?.Rules != null && _world.Config.Rules.dayGuestSpawnCount > 0)
                ? _world.Config.Rules.dayGuestSpawnCount
                : 4;

            _guestSpawner.SpawnGuests(guestCountToday);
            
            // 3) 刷新鬼商店
            _ghostShop.RefreshDaily(dayIndex);
        }
        
        // UI接口
        public bool ShopReroll(int dayIndex) => _ghostShop.TryReroll(dayIndex);
        public bool ShopBuy(int slot, out string newGhostId) => _ghostShop.TryBuy(slot, out newGhostId);
    }


    // ===== 进程推进（难度/解锁曲线）=====
    public class ProgressionSystem
    {
        private readonly World _world;
        public ProgressionSystem(World world) { _world = world; }
    }
}
