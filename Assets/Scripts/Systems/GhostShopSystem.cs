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

        private readonly Data.ConfigDatabase _db;

        public GhostShopSystem(World world, Data.ConfigDatabase db)
        {
            _world = world;
            _db = db;
        }

        // 从运行时 Id 提取“配置 id”前缀（与 PawnView 的解析规则保持一致）
        private static string ExtractBaseConfigId(string runtimeId)
        {
            if (string.IsNullOrEmpty(runtimeId)) return null;
            int cut = runtimeId.IndexOfAny(new[] { '#', '@', ':' });
            return cut > 0 ? runtimeId.Substring(0, cut) : runtimeId;
        }
        
        public void RefreshDaily(int dayIndex, bool force = false)
        {
            var rules = _world.Config?.Rules;
            if (rules == null) return;
            if (!force && _world.Shop.DayLastRefreshed == dayIndex) return;

            int slots = Math.Max(1, rules.ghostShopSlots);
            GenerateOffers(dayIndex, slots, rules.ghostShopUniqueMains);
        }

        // 要求：直接读 world 中的 ghost 并随机，从这些实例衍生出商店条目
        private void GenerateOffers(int dayIndex, int count, bool uniqueMains)
        {
            _world.Shop.Offers.Clear();

            if (_world == null || _world.Ghosts == null || _world.Ghosts.Count == 0 || count <= 0)
                return;

            // 候选来自“世界中已经存在的鬼”
            var candidates = _world.Ghosts.ToList();
            var usedMains = new HashSet<FearTag>();

            for (int i = 0; i < count; i++)
            {
                Ghost chosen = null;

                if (uniqueMains)
                {
                    // 优先覆盖更多 Main
                    var mainCandidates = candidates.Where(g => !usedMains.Contains(g.Main)).ToList();
                    if (mainCandidates.Count > 0)
                        chosen = mainCandidates[_rng.Next(mainCandidates.Count)];
                }

                // 不足时退化到任意随机以补满
                if (chosen == null)
                    chosen = candidates[_rng.Next(candidates.Count)];

                // 从运行时 Id 还原配置 id 前缀，供后续购买/展示配对
                string cfgId = ExtractBaseConfigId(chosen.Id);
                if (string.IsNullOrEmpty(cfgId) || !_db.Ghosts.TryGetValue(cfgId, out var cfg))
                    continue; // 若对应配置不存在则跳过
                
                string offerId = $"{cfgId}@offer_{dayIndex}_{i + 1}";

                _world.Shop.Offers.Add(new GhostOffer
                {
                    OfferId  = offerId,
                    ConfigId = cfgId,     // 依然保存配置 id，购买时使用
                    Main     = chosen.Main
                });

                usedMains.Add(chosen.Main);
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
                Sub   = null,
                State = GhostState.Idle,
                DaysForcedRest = 0
            });

            _world.Shop.Offers.RemoveAt(slotIndex);
            return true;
        }
    }
}
