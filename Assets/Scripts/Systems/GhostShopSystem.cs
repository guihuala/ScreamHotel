using System;
using System.Collections.Generic;
using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Core;
using UnityEngine;
using Random = System.Random;

namespace ScreamHotel.Systems
{
    public class GhostShopSystem
    {
        private readonly World _world;
        private readonly Random _rng = new();

        private readonly Data.ConfigDatabase _db;

        public GhostShopSystem(World world, Data.ConfigDatabase db)
        {
            _world = world;
            _db = db;
        }
  
        private void GenerateOffers(int dayIndex, int count, bool unique)
        {
            _world.Shop.Offers.Clear();

            // 从数据库拿所有鬼的配置id
            var keys = _db.Ghosts.Keys.ToList();
            if (keys.Count == 0)
            {
                Debug.LogWarning("No GhostConfig in Database."); 
                _world.Shop.DayLastRefreshed = dayIndex;
                return;
            }

            var used = new HashSet<string>();
            for (int i = 0; i < count; i++)
            {
                string cfgId;
                if (unique)
                {
                    // 唯一：不重复 configId
                    var candidates = keys.Where(k => !used.Contains(k)).ToList();
                    if (candidates.Count == 0) break;
                    cfgId = candidates[_rng.Next(candidates.Count)];
                    used.Add(cfgId);
                }
                else
                {
                    cfgId = keys[_rng.Next(keys.Count)];
                }

                var cfg = _db.Ghosts[cfgId];

                _world.Shop.Offers.Add(new GhostOffer
                {
                    OfferId  = $"Offer_{dayIndex}_{i + 1}",
                    ConfigId = cfgId,
                    Main     = cfg.main     // 供UI展示；购买时也能一致
                });
            }

            _world.Shop.DayLastRefreshed = dayIndex;
        }
    
        public void RefreshDaily(int dayIndex, bool force = false)
        {
            var rules = _world.Config?.Rules;
            if (rules == null) return;
            if (!force && _world.Shop.DayLastRefreshed == dayIndex) return;

            int slots = Math.Max(1, rules.ghostShopSlots);
            GenerateOffers(dayIndex, slots, unique: rules.ghostShopUniqueMains);
        }
        
        public bool TryReroll(int dayIndex)
        {
            var rules = _world.Config?.Rules;
            if (rules == null) return false;

            int remaining = _world.Shop.Offers.Count;
            if (remaining <= 0) return false;                  // 没货可刷，直接失败

            if (_world.Economy.Gold < rules.ghostShopRerollCost) return false;
            _world.Economy.Gold -= rules.ghostShopRerollCost;
            EventBus.Raise(new GoldChanged(_world.Economy.Gold));

            GenerateOffers(dayIndex, remaining, unique: rules.ghostShopUniqueMains);
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

            // 从报价指向的配置生成
            if (!_db.Ghosts.TryGetValue(offer.ConfigId, out var cfg))
            {
                Debug.LogError($"Ghost config not found: {offer.ConfigId}");
                return false;
            }

            _world.Economy.Gold -= rules.ghostShopPrice;
            EventBus.Raise(new GoldChanged(_world.Economy.Gold));

            newGhostId = $"G_shop_{DateTime.UtcNow.Ticks % 1000000:000000}";
            _world.Ghosts.Add(new Ghost
            {
                Id    = newGhostId,
                Main  = cfg.main,
                Sub   = null,
                State = GhostState.Idle,
                DaysForcedRest = 0
            });

            _world.Shop.Offers.RemoveAt(slotIndex);
            return true;
        }
    }
}