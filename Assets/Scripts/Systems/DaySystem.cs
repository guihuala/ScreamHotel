using System.Linq;
using System.Collections.Generic;
using ScreamHotel.Domain;
using ScreamHotel.Core;
using UnityEngine;

namespace ScreamHotel.Systems
{
    public class DayPhaseSystem
    {
        private readonly World _world;
        private readonly DayGuestSpawner _guestSpawner;
        private readonly GhostShopSystem _ghostShop;

        public DayPhaseSystem(World world, Data.ConfigDatabase db, DayGuestSpawner sharedSpawner = null)
        {
            _world = world;
            _guestSpawner = sharedSpawner ?? new DayGuestSpawner(world, db);
            _ghostShop = new GhostShopSystem(world, db);
        }
        
        public void PrepareDay(int dayIndex)
        {
            // 1) 清除上一日的所有客人数据
            Debug.Log("[DayPhaseSystem] Preparing for a new day...");
    
            // 清空 _world.Guests 来移除上一天的所有客人
            _world.Guests.Clear();  // 清空 _world.Guests 列表

            _guestSpawner.ClearAll(); // 清空待接受和已接受的客人
            
            foreach (var r in _world.Rooms) r.AssignedGuestIds.Clear();// 清空房间

            // 2) 刷新商店
            _ghostShop.RefreshDaily(dayIndex);

            // 3) 生成“候选顾客”（不写入 world）
            int baseCount = (_world.Config?.Rules != null && _world.Config.Rules.dayGuestSpawnCount > 0)
                ? _world.Config.Rules.dayGuestSpawnCount
                : 3;

            var progression = new ProgressionSystem(_world, _world.Config);
            float t = progression.GetHardGuestRatio(dayIndex);

            int scaledCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * Mathf.Lerp(1f, 2.5f, t)));

            _guestSpawner.GenerateCandidates(scaledCount);

            Debug.Log($"[DayPhaseSystem] Pending guests today: {scaledCount} (base={baseCount}, t={t:0.00})");
        }


        public bool ShopReroll(int dayIndex)                 => _ghostShop.TryReroll(dayIndex);
        public bool ShopBuy(int slot, out string newGhostId) => _ghostShop.TryBuy(slot, out newGhostId);
    }
}
