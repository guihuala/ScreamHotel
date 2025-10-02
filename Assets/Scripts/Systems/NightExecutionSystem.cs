using System;
using System.Collections.Generic;
using System.Linq;
using ScreamHotel.Core;
using ScreamHotel.Domain;

namespace ScreamHotel.Systems
{
    public class NightExecutionSystem
    {
        private readonly World _world;

        public NightExecutionSystem(World world) => _world = world;

        public NightResolved ResolveNight()
        {
            var result = new NightResolved { RoomResults = new List<RoomNightResult>() };
            int totalGold = 0;

            foreach (var room in _world.Rooms)
            {
                // 1) 收集该房间“有效恐惧集合”（鬼 Main/Sub + 房间Tag）
                var effectiveTags = CollectEffectiveFearTags(room);

                // 2) 针对该房间分配的每位客人逐个结算
                var rr = new RoomNightResult { RoomId = room.Id, GuestResults = new List<GuestNightResult>() };

                foreach (var guestId in room.AssignedGuestIds)
                {
                    var g = _world.Guests.FirstOrDefault(x => x.Id == guestId);
                    if (g == null) continue;

                    var vulnerabilities = GetGuestVulnerabilities(g);
                    int hits = effectiveTags.Count(t => vulnerabilities.Contains(t));
                    int baseFee = GetGuestBaseFee(g);

                    int gold = (hits >= 1) ? baseFee * hits : 0;
                    totalGold += gold;

                    rr.GuestResults.Add(new GuestNightResult
                    {
                        GuestId = g.Id,
                        Hits = hits,
                        BaseFee = baseFee,
                        GoldEarned = gold,
                        EffectiveTags = new List<FearTag>(effectiveTags),
                        Immunities = new List<FearTag>()
                    });
                }

                result.RoomResults.Add(rr);
            }

            // 3) 加钱并返回结果
            _world.Economy.Gold += totalGold;
            EventBus.Raise(new GoldChanged(_world.Economy.Gold));
            result.TotalGold = totalGold;

            EventBus.Raise(result);
            return result;
        }

        // ======= Helpers =======

        /// <summary>
        /// 房间内所有鬼的 Main/Sub ∪ 房间 Tag（若有）
        /// </summary>
        private HashSet<FearTag> CollectEffectiveFearTags(Room room)
        {
            var set = new HashSet<FearTag>();

            // 鬼
            foreach (var gid in room.AssignedGhostIds)
            {
                var gh = _world.Ghosts.FirstOrDefault(x => x.Id == gid);
                if (gh == null) continue;
                set.Add(gh.Main);
                if (gh.Sub.HasValue) set.Add(gh.Sub.Value);
            }

            // 房间 Tag（Lv2/Lv3 可能带装饰恐惧）
            if (room.RoomTag.HasValue) set.Add(room.RoomTag.Value);

            return set;
        }

        private HashSet<FearTag> GetGuestVulnerabilities(Guest g)
        {
            // Domain.Guest.Fears 既已是“怕的东西/易感集合”
            return g.Fears != null ? new HashSet<FearTag>(g.Fears) : new HashSet<FearTag>();
        }

        private int GetGuestBaseFee(Guest g)
        {
            return (g.BaseFee > 0) ? g.BaseFee : 40;
        }
    }

    // ====== 结果结构 ======
    public class NightResolved : IGameEvent
    {
        public int TotalGold;
        public List<RoomNightResult> RoomResults;
    }

    public class RoomNightResult
    {
        public string RoomId;
        public List<GuestNightResult> GuestResults;
    }

    public class GuestNightResult
    {
        public string GuestId;
        public int Hits;
        public int BaseFee;
        public int GoldEarned;
        public List<FearTag> EffectiveTags; // 鬼/房间构成的全集
        public List<FearTag> Immunities;    // 客人免疫集合（用于调试展示）
    }
}

