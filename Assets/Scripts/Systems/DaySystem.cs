using System.Linq;
using System.Collections.Generic;
using ScreamHotel.Domain;
using ScreamHotel.Core;
using UnityEngine;

namespace ScreamHotel.Systems
{
    /// <summary>
    /// ===== 白天准备（刷新当天资源）=====
    /// - 不再把顾客直接写入 World.Guests
    /// - 仅生成“候选顾客”，供 HUD 面板接受/拒绝
    /// - 进入 NightShow 时再把“已接受顾客”写入 World（允许分配）
    /// </summary>
    public class DayPhaseSystem
    {
        private readonly World _world;
        private readonly DayGuestSpawner _guestSpawner;
        private readonly GhostShopSystem _ghostShop;

        public DayPhaseSystem(World world, Data.ConfigDatabase db)
        {
            _world        = world;
            _guestSpawner = new DayGuestSpawner(world, db);
            _ghostShop    = new GhostShopSystem(world, db);
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

            // 2) 刷新鬼商店（保留原有逻辑）
            _ghostShop.RefreshDaily(dayIndex);

            // 3) 生成“候选顾客”（不写入 world）
            int guestCountToday =
                (_world.Config?.Rules != null && _world.Config.Rules.dayGuestSpawnCount > 0)
                    ? _world.Config.Rules.dayGuestSpawnCount
                    : 3; // 兜底
            _guestSpawner.GenerateCandidates(guestCountToday);

            Debug.Log($"[DayPhase] Generated pending guests: {_guestSpawner.PendingCount}");
        }

        // —— NightShow 切换时调用：仅把“已接受”顾客写入世界，允许分配 ——
        public int FlushAcceptedGuestsToWorld()
        {
            int n = _guestSpawner.FlushAcceptedToWorld();
            Debug.Log($"[DayPhase] NightShow flush accepted -> {n}");
            return n;
        }

        // —— HUD/面板访问接口（只读列表 + 接受/拒绝）——
        public IReadOnlyList<Guest> GetPendingGuests()  => _guestSpawner.Pending;
        public IReadOnlyList<Guest> GetAcceptedGuests() => _guestSpawner.Accepted;
        public int GetPendingCount()  => _guestSpawner.PendingCount;
        public int GetAcceptedCount() => _guestSpawner.AcceptedCount;

        public bool ApproveGuest(string guestId) => _guestSpawner.Accept(guestId);
        public bool RejectGuest(string guestId)  => _guestSpawner.Reject(guestId);

        // —— 商店接口保持不变 —— 
        public bool ShopReroll(int dayIndex)                 => _ghostShop.TryReroll(dayIndex);
        public bool ShopBuy(int slot, out string newGhostId) => _ghostShop.TryBuy(slot, out newGhostId);
    }


    /// <summary>
    /// ===== 进程推进（难度/解锁曲线）=====
    /// </summary>
    public class ProgressionSystem
    {
        private readonly World _world;
        public ProgressionSystem(World world) { _world = world; }
    }
}
