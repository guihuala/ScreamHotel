using System;
using System.Collections.Generic;
using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Core;

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

        // 每日刷新
        public void RefreshDaily(int dayIndex, bool force = false)
        {
            var rules = _world.Config?.Rules;
            if (rules == null) return;

            if (!force && _world.Shop.DayLastRefreshed == dayIndex) return;

            _world.Shop.Offers.Clear();
            int slots = Math.Max(1, rules.ghostShopSlots);

            var mains = ((FearTag[])Enum.GetValues(typeof(FearTag))).ToList();

            // 抽样：是否要求不重复
            for (int i = 0; i < slots; i++)
            {
                FearTag main;
                if (rules.ghostShopUniqueMains && mains.Count > 0)
                {
                    int idx = _rng.Next(mains.Count);
                    main = mains[idx];
                    mains.RemoveAt(idx);
                }
                else
                {
                    var all = (FearTag[])Enum.GetValues(typeof(FearTag));
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

        // 刷新当前日：扣钱并重抽
        public bool TryReroll(int dayIndex)
        {
            var rules = _world.Config?.Rules;
            if (rules == null) return false;
            if (_world.Economy.Gold < rules.ghostShopRerollCost) return false;

            _world.Economy.Gold -= rules.ghostShopRerollCost;
            EventBus.Raise(new GoldChanged(_world.Economy.Gold));

            RefreshDaily(dayIndex, force: true);
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