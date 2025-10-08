using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using UnityEngine;

namespace ScreamHotel.Systems
{
    public class NightExecutionSystem
    {
        private readonly World _world;

        public NightExecutionSystem(World world) => _world = world;

        public ExecNightResolved ResolveNight()
        {
            var result = new ExecNightResolved
            {
                RoomResults = new List<RoomNightResult>()
            };

            int totalGold = 0;
            int guestsTotal = 0;
            int guestsScared = 0;
            HashSet<string> ghostSet = new HashSet<string>();

            foreach (var room in _world.Rooms)
            {
                var effectiveTags = CollectEffectiveFearTags(room);
                var rr = new RoomNightResult
                {
                    RoomId = room.Id,
                    GuestResults = new List<GuestNightResult>()
                };

                // 统计鬼怪出场
                foreach (var gid in room.AssignedGhostIds)
                    if (!string.IsNullOrEmpty(gid))
                        ghostSet.Add(gid);

                // 逐客结算
                foreach (var guestId in room.AssignedGuestIds)
                {
                    var g = _world.Guests.FirstOrDefault(x => x.Id == guestId);
                    if (g == null) continue;

                    guestsTotal++;
                    var vulnerabilities = GetGuestVulnerabilities(g);
                    int hits = effectiveTags.Count(t => vulnerabilities.Contains(t));
                    int baseFee = GetGuestBaseFee(g);

                    int gold = (hits >= 1) ? baseFee * hits : 0;
                    totalGold += gold;
                    if (hits > 0) guestsScared++;

                    rr.GuestResults.Add(new GuestNightResult
                    {
                        GuestId = g.Id,
                        Hits = hits,
                        GoldEarned = gold,
                        EffectiveTags = new List<FearTag>(effectiveTags),
                    });
                }

                result.RoomResults.Add(rr);
            }

            _world.Economy.Gold += totalGold;
            EventBus.Raise(new GoldChanged(_world.Economy.Gold));

            // 汇总字段
            result.TotalGold = totalGold;
            result.GuestsTotal = guestsTotal;
            result.GuestsScared = guestsScared;
            result.GhostsUsed = ghostSet.Count;
            result.RoomCount = _world.Rooms.Count;
            result.ScareRate = guestsTotal > 0 ? (float)guestsScared / guestsTotal : 0f;
            
            EventBus.Raise(result);
            return result;
        }

        private HashSet<FearTag> CollectEffectiveFearTags(Room room)
        {
            var set = new HashSet<FearTag>();

            foreach (var gid in room.AssignedGhostIds)
            {
                var gh = _world.Ghosts.FirstOrDefault(x => x.Id == gid);
                if (gh == null) continue;
                set.Add(gh.Main);
                if (gh.Sub.HasValue) set.Add(gh.Sub.Value);
            }

            if (room.RoomTag.HasValue) set.Add(room.RoomTag.Value);

            return set;
        }

        private HashSet<FearTag> GetGuestVulnerabilities(Guest g)
        {
            return g.Fears != null ? new HashSet<FearTag>(g.Fears) : new HashSet<FearTag>();
        }

        private int GetGuestBaseFee(Guest g)
        {
            return (g.BaseFee > 0) ? g.BaseFee : 40;
        }
    }

    // ====== 结果结构 ======
    public class ExecNightResolved : IGameEvent
    {
        public int TotalGold;
        public int GuestsTotal;
        public int GuestsScared;
        public int GhostsUsed;
        public int RoomCount;
        public float ScareRate;
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
        public int GoldEarned;
        public List<FearTag> EffectiveTags; // 鬼/房间构成的全集
    }
}