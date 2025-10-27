using ScreamHotel.Domain;
using UnityEngine;

namespace ScreamHotel.Systems
{
    public class ProgressionSystem
    {
        private readonly World _world;
        private readonly Data.ConfigDatabase _db;
        private readonly AnimationCurve _guestMixCurve; // 高难客人比例曲线[0,1]->[0,1]

        public ProgressionSystem(World world, Data.ConfigDatabase db)
        {
            _world = world;
            _db = db;

            // 从 ProgressionConfig 里取曲线；没有就用线性兜底
            var prog = _db?.Progression as ScreamHotel.Data.ProgressionConfig;
            _guestMixCurve = prog != null && prog.guestMixCurve != null
                ? prog.guestMixCurve
                : AnimationCurve.Linear(0, 0f, 1, 1f);
        }

        /// <summary>把 dayIndex 映射到 [0,1] 进度，并给出“高难客人占比”</summary>
        public float GetHardGuestRatio(int dayIndex)
        {
            var rules = _world?.Config?.Rules;
            int totalDays = Mathf.Max(1, rules != null ? rules.totalDays : 14);
            // 归一化进度：第1天≈0，第N天≈1
            float t = Mathf.Clamp01(totalDays <= 1 ? 1f : (dayIndex - 1) / (float)(totalDays - 1));
            return Mathf.Clamp01(_guestMixCurve.Evaluate(t));
        }
    }
}