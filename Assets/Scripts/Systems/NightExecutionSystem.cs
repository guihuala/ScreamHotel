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
            HashSet<string> servedGuestSet = new HashSet<string>(); // 已接待集合

            foreach (var room in _world.Rooms)
            {
                var effectiveTags = CollectEffectiveFearTags(room);
                var rr = new RoomNightResult
                {
                    RoomId = room.Id,
                    GuestResults = new List<GuestNightResult>()
                };

                foreach (var gid in room.AssignedGhostIds)
                    if (!string.IsNullOrEmpty(gid))
                        ghostSet.Add(gid);

                foreach (var guestId in room.AssignedGuestIds)
                {
                    servedGuestSet.Add(guestId); // 记录已接待
                    var g = _world.Guests.FirstOrDefault(x => x.Id == guestId);
                    if (g == null) continue;

                    guestsTotal++;
                    var immunities = GetGuestImmunities(g);
                    int hits = effectiveTags.Count(t => !immunities.Contains(t));

                    int baseFee = GetGuestBaseFee(g);

                    int gold = (hits >= 1) ? baseFee * hits : 0;
                    totalGold += gold;
                    if (hits > 0) guestsScared++;

                    rr.GuestResults.Add(new GuestNightResult
                    {
                        GuestId = g.Id,
                        Hits = hits,
                    });
                }

                result.RoomResults.Add(rr);
            }

            _world.Economy.Gold += totalGold;
            EventBus.Raise(new GoldChanged(_world.Economy.Gold));

            // === 失败数 & 未接待数 ===
            int assignedFails = Mathf.Max(0, guestsTotal - guestsScared);
            int unserved = Mathf.Max(0, _world.Guests.Count - servedGuestSet.Count);
            
            result.TotalGold = totalGold;
            result.GuestsTotal = guestsTotal;
            result.GuestsScared = guestsScared;
            result.GhostsUsed = ghostSet.Count;
            result.RoomCount = _world.Rooms.Count;
            result.ScareRate = guestsTotal > 0 ? (float)guestsScared / guestsTotal : 0f;
            result.UnservedGuests = unserved;
            result.AssignedFails = assignedFails;

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

        private HashSet<FearTag> GetGuestImmunities(Guest g)
        {
            return g.Immunities != null ? new HashSet<FearTag>(g.Immunities) : new HashSet<FearTag>();
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

        public int AssignedFails; // 已接待但未被吓到的数量
        public int UnservedGuests; // 今日未接待的客人数
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
    }
}