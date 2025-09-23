using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Core;

namespace ScreamHotel.Systems
{
    // ===== 白天准备（强制休息递减、刷新当天资源）=====
    public class DayPhaseSystem
    {
        private readonly World _world;
        public DayPhaseSystem(World world) { _world = world; }

        public void PrepareDay(int day)
        {
            // 强制休息递减
            foreach (var g in _world.Ghosts)
            {
                if (g.DaysForcedRest > 0)
                {
                    g.DaysForcedRest--;
                    g.State = (g.DaysForcedRest == 0) ? GhostState.Idle : GhostState.Resting;
                }
            }

            // TODO：生成当日客源池 / 候选鬼怪 / 商店刷新等
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
