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

        /// <summary>白天初始化：清分配/状态、刷新商店、生成当日候选顾客</summary>
        public void PrepareDay(int dayIndex)
        {
            // 1) 清当日分配/状态
            foreach (var r in _world.Rooms) r.AssignedGuestIds.Clear();
            foreach (var g in _world.Ghosts)
            {
                if (g.State == GhostState.Working) g.State = GhostState.Idle;
            }

            // 2) 刷新鬼商店
            _ghostShop.RefreshDaily(dayIndex);

            // 3) 生成“候选顾客”（不写入 world）
            int baseCount =
                (_world.Config?.Rules != null && _world.Config.Rules.dayGuestSpawnCount > 0)
                    ? _world.Config.Rules.dayGuestSpawnCount
                    : 3; // 兜底

            // 使用进度系统得到难度进度 t ∈ [0,1]
            var progression = new ProgressionSystem(_world, _world.Config);

            // 这里复用已有的“进度→曲线”接口，取名 hardRatio 但仅用于数量缩放
            float t = progression.GetHardGuestRatio(dayIndex);

            // 线性从 1.0x ~ 1.75x 放大人数
            int scaledCount = Mathf.Max(1, Mathf.RoundToInt(baseCount * Mathf.Lerp(1f, 1.75f, t)));

            // 只按数量生成；不改变类型与参数
            _guestSpawner.GenerateCandidates(scaledCount);

            Debug.Log($"[DayPhase] Pending guests today: {scaledCount} (base={baseCount}, t={t:0.00})");
        }
        
        // —— 商店接口保持不变 —— 
        public bool ShopReroll(int dayIndex)                 => _ghostShop.TryReroll(dayIndex);
        public bool ShopBuy(int slot, out string newGhostId) => _ghostShop.TryBuy(slot, out newGhostId);
    }
}
