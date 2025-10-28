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
            
            var prog = _db?.Progression;
            _guestMixCurve = prog != null && prog.guestMixCurve != null
                ? prog.guestMixCurve
                : AnimationCurve.Linear(0, 0f, 1, 1f);
        }
        
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