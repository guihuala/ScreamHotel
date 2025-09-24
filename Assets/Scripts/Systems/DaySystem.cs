using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Core;

namespace ScreamHotel.Systems
{
    // ===== 白天准备（刷新当天资源）=====
// Systems/DaySystem.cs 里 DayPhaseSystem（追加/修改）
    public class DayPhaseSystem
    {
        private readonly World _world;
        private readonly DayGuestSpawner _guestSpawner;
        

        public DayPhaseSystem(World world)
        {
            _world = world;
            _guestSpawner = new DayGuestSpawner(world);
            
        }

        public void PrepareDay(int dayIndex)
        {
            // 1) 清理/重置每日状态
            foreach (var r in _world.Rooms) r.AssignedGuestIds.Clear();
            foreach (var g in _world.Ghosts)
            {
                if (g.State == GhostState.Working) g.State = GhostState.Idle;
                if (g.DaysForcedRest > 0) g.DaysForcedRest--;
                if (g.State == GhostState.Injured && g.DaysForcedRest <= 0) g.State = GhostState.Idle;
            }

            // 2) 读取规则中的当日刷客/刷鬼数量
            int guestCountToday = (_world.Config?.Rules != null && _world.Config.Rules.dayGuestSpawnCount > 0)
                ? _world.Config.Rules.dayGuestSpawnCount : 4;
            int ghostCountToday = (_world.Config?.Rules != null && _world.Config.Rules.dayGhostSpawnCount > 0)
                ? _world.Config.Rules.dayGhostSpawnCount : 0;
            
        }
    }


    // ===== 进程推进（难度/解锁曲线）=====
    public class ProgressionSystem
    {
        private readonly World _world;
        public ProgressionSystem(World world) { _world = world; }

        public void Advance(NightResults results, int dayIndex)
        {
            // TODO：根据 ProgressionConfig 调整可用恐惧池、解锁特殊客人/功能、切换价格档等
        }
    }
}
