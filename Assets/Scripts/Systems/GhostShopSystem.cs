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

        public GhostShopSystem(World world)
        {
            _world = world;
        }
        
        private void GenerateOffers(int dayIndex, int count, bool unique)
        {
            _world.Shop.Offers.Clear();
            var all = (FearTag[])Enum.GetValues(typeof(FearTag));
            var pool = all.ToList();

            for (int i = 0; i < count; i++)
            {
                FearTag main;
                if (unique && pool.Count > 0)
                {
                    int idx = _rng.Next(pool.Count);
                    main = pool[idx];
                    pool.RemoveAt(idx);
                }
                else
                {
                    main = all[_rng.Next(all.Length)];
                }

                _world.Shop.Offers.Add(new GhostOffer
                {
                    OfferId = $"Offer_{dayIndex}_{i + 1}",
                    Main = main
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
            Debug.Log($"TryBuy {slotIndex}");
            newGhostId = null;
            var rules = _world.Config?.Rules;
            if (rules == null) return false;
            if (slotIndex < 0 || slotIndex >= _world.Shop.Offers.Count) return false;
            var offer = _world.Shop.Offers[slotIndex];

            if (_world.Economy.Gold < rules.ghostShopPrice) return false;

            _world.Economy.Gold -= rules.ghostShopPrice;
            EventBus.Raise(new GoldChanged(_world.Economy.Gold));

            // 生成一只新鬼加入世界
            newGhostId = $"G_shop_{DateTime.UtcNow.Ticks % 1000000:000000}";
            _world.Ghosts.Add(new Ghost
            {
                Id = newGhostId,
                Main = offer.Main,
                Sub = null,
                State = GhostState.Idle,
                DaysForcedRest = 0
            });

            _world.Shop.Offers.RemoveAt(slotIndex);
            return true;
        }
    }
}