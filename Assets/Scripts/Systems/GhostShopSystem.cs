using System;
using System.Collections.Generic;
using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Core;
using ScreamHotel.Data;
using UnityEngine;
using Random = System.Random;

namespace ScreamHotel.Systems
{
    public class GhostShopSystem
    {
        private readonly World _world;
        private readonly Random _rng = new();

        private readonly ConfigDatabase _db;

        public GhostShopSystem(World world, Data.ConfigDatabase db)
        {
            _world = world;
            _db = db;
        }
        
        public void RefreshDaily(int dayIndex, bool force = false)
        {
            var rules = _world.Config?.Rules;
            if (rules == null) return;
            if (!force && _world.Shop.DayLastRefreshed == dayIndex) return;

            int slots = Math.Max(1, rules.ghostShopSlots);
            GenerateOffers(dayIndex, slots, rules.ghostShopUniqueMains);
        }

        // 用配置全集生成每日商店
        private void GenerateOffers(int dayIndex, int count, bool uniqueMains)
        {
            _world.Shop.Offers.Clear();
            if (_db == null || _db.Ghosts == null || _db.Ghosts.Count == 0 || count <= 0)
                return;

            // 候选 = 全量配置（来自 DataManager.configSet.ghosts，经 Initialize 灌入 Database）
            var allCfgs = _db.Ghosts.Values.ToList();

            var usedMains = new HashSet<FearTag>();
            for (int i = 0; i < count; i++)
            {
                ScreamHotel.Data.GhostConfig cfg = null;

                if (uniqueMains)
                {
                    // 尽量覆盖更多 Main
                    var mainCandidates = allCfgs.Where(c => !usedMains.Contains(c.main)).ToList();
                    if (mainCandidates.Count > 0)
                        cfg = mainCandidates[_rng.Next(mainCandidates.Count)];
                }

                // 不足时退化到任意随机
                if (cfg == null)
                    cfg = allCfgs[_rng.Next(allCfgs.Count)];

                if (cfg == null || string.IsNullOrEmpty(cfg.id))
                    continue;

                string cfgId = cfg.id;
                string offerId = $"{cfgId}@offer_{dayIndex}_{i + 1}";

                _world.Shop.Offers.Add(new GhostOffer
                {
                    OfferId  = offerId,
                    ConfigId = cfgId,   // 购买时仍按配置id落地
                    Main     = cfg.main
                });

                usedMains.Add(cfg.main);
            }

            _world.Shop.DayLastRefreshed = dayIndex;
        }

        public bool TryReroll(int dayIndex)
        {
            var rules = _world.Config?.Rules;
            if (rules == null) return false;

            int remaining = _world.Shop.Offers.Count;
            if (remaining <= 0) return false;

            if (_world.Economy.Gold < rules.ghostShopRerollCost) return false;
            _world.Economy.Gold -= rules.ghostShopRerollCost;
            EventBus.Raise(new GoldChanged(_world.Economy.Gold));

            GenerateOffers(dayIndex, remaining, rules.ghostShopUniqueMains);
            return true;
        }

        // 购买指定槽位：扣钱，加入世界鬼列表，并从货架移除
        public bool TryBuy(int slotIndex, out string newGhostId)
        {
            newGhostId = null;
            var rules = _world.Config?.Rules;
            if (rules == null) return false;
            if (slotIndex < 0 || slotIndex >= _world.Shop.Offers.Count) return false;

            var offer = _world.Shop.Offers[slotIndex];
            if (_world.Economy.Gold < rules.ghostShopPrice) return false;

            // 从报价的 ConfigId 取配置（ConfigId 来自世界实例 Id 的前缀）
            if (!_db.Ghosts.TryGetValue(offer.ConfigId, out var cfg))
            {
                Debug.LogError($"Ghost config not found: {offer.ConfigId}");
                return false;
            }

            _world.Economy.Gold -= rules.ghostShopPrice;
            EventBus.Raise(new GoldChanged(_world.Economy.Gold));

            // 运行时实例 Id 必须带上“配置 id”前缀，PawnView 可据此前缀解析并匹配外观
            newGhostId = $"{cfg.id}#shop_{DateTime.UtcNow.Ticks % 1000000:000000}";

            _world.Ghosts.Add(new Ghost
            {
                Id    = newGhostId,
                Main  = cfg.main,
                State = GhostState.Idle,
            });

            _world.Shop.Offers.RemoveAt(slotIndex);
            return true;
        }
    }
}
