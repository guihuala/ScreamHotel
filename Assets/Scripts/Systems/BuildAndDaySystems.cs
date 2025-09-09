using System.Linq;
using ScreamHotel.Domain;

namespace ScreamHotel.Systems
{
    // 房间购买、升级与经济结算
    public class BuildSystem
    {
        private readonly World _world;
        public BuildSystem(World world) { _world = world; }

        public void ApplySettlement(NightResults results)
        {
            
        }

        public bool TryUpgradeRoom(string roomId)
        {
            var r = _world.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null) return false;
            var tier = _world.Config.RoomPrices.Values.First();
            if (r.Level == 1 && _world.Economy.Gold >= tier.upgradeToLv2)
            {
                _world.Economy.Gold -= tier.upgradeToLv2; r.Level = 2; r.Capacity = 1; r.RoomTag = FearTag.Darkness; return true;
            }
            if (r.Level == 2 && _world.Economy.Gold >= tier.upgradeToLv3)
            {
                _world.Economy.Gold -= tier.upgradeToLv3; r.Level = 3; r.Capacity = 2; return true;
            }
            return false;
        }
    }

    // 每天早晨的刷新逻辑
    public class DayPhaseSystem
    {
        private readonly World _world;
        public DayPhaseSystem(World world) { _world = world; }

        public void PrepareDay(int day)
        {
            foreach (var g in _world.Ghosts)
            {
                if (g.DaysForcedRest > 0)
                {
                    g.DaysForcedRest--;
                    if (g.DaysForcedRest == 0) g.State = GhostState.Idle;
                    else g.State = GhostState.Resting;
                }
            }
        }
    }

    // 难度与曲线推进（随天数解锁恐惧池、特殊客人），与ProgressionConfig对接
    public class ProgressionSystem
    {
        private readonly World _world;

        public ProgressionSystem(World world)
        {
            _world = world;
        }

        public void Advance(NightResults results, int dayIndex)
        {
        }
    }
}
