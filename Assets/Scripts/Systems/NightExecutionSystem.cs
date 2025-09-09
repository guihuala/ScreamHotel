using System;
using System.Collections.Generic;
using System.Linq;
using ScreamHotel.Domain;

namespace ScreamHotel.Systems
{
    // 夜晚执行
    public class NightExecutionSystem
    {
        private readonly World _world;
        public NightExecutionSystem(World world) { _world = world; }

        public NightResults ResolveNight(int rngSeed)
        {
            var rand = new Random(rngSeed);
            var results = new NightResults();
            var rules = _world.Config.Rules;

            foreach (var room in _world.Rooms)
            {
                var guestType = _world.Config.GuestTypes.Values.ElementAt(rand.Next(_world.Config.GuestTypes.Count));
                var required = guestType.barMax * guestType.requiredPercent;

                float total = 0f;
                foreach (var gid in room.AssignedGhostIds)
                {
                    var ghost = _world.Ghosts.First(x => x.Id == gid);
                    var bonus = 0f;
                    if (guestType.fears.Contains(ghost.Main)) bonus += rules.mainBonus;
                    if (ghost.Sub.HasValue && guestType.fears.Contains(ghost.Sub.Value)) bonus += rules.subBonus;
                    if (room.RoomTag.HasValue && guestType.fears.Contains(room.RoomTag.Value)) bonus += rules.roomBonus;

                    var fatiguePenalty = ghost.Fatigue > 0.5f ? rules.highFatiguePenalty : 0f;
                    var contrib = ghost.BaseScare * (1 + bonus) * (1 - ghost.Fatigue) * (1 - fatiguePenalty);
                    total += contrib;
                }

                var success = total >= required;
                var goldMul = success ? Math.Min(1f, total / required) : Math.Max(_world.Config.Rules.minGoldMultiplier, total / required);
                var gold = (int)Math.Max(0, Math.Round(guestType.baseFee * goldMul));

                var counter = false;
                if (!success && guestType.counterChance > 0f)
                {
                    counter = rand.NextDouble() < guestType.counterChance;
                }

                foreach (var gid in room.AssignedGhostIds)
                {
                    var ghost = _world.Ghosts.First(x => x.Id == gid);
                    ghost.Fatigue = Math.Clamp(ghost.Fatigue + rules.fatiguePerNight, 0f, 1f);
                    if (counter)
                    {
                        ghost.State = GhostState.Injured;
                        ghost.DaysForcedRest = Math.Max(ghost.DaysForcedRest, rules.forcedRestDaysOnCounter);
                    }
                    else ghost.State = GhostState.Idle;
                }

                results.RoomDetails.Add(new RoomResult
                {
                    RoomId = room.Id,
                    GuestTypeId = guestType.id,
                    TotalScare = total,
                    Required = required,
                    Gold = gold,
                    Counter = counter
                });

                _world.Economy.Gold += gold;
                room.AssignedGhostIds.Clear();
            }

            return results;
        }
    }

    public class NightResults
    {
        public readonly System.Collections.Generic.List<RoomResult> RoomDetails = new();
        public int TotalGold => RoomDetails.Sum(r => r.Gold);
    }

    public struct RoomResult
    {
        public string RoomId;
        public string GuestTypeId;
        public float TotalScare;
        public float Required;
        public int Gold;
        public bool Counter;
    }
}
