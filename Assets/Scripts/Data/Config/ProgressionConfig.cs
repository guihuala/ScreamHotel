using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/Progression", fileName = "Progression")]
    public class ProgressionConfig : ScriptableObject
    {
        [System.Serializable]
        public struct DayCurve
        {
            public int day;
            public int availableFearCount;
        }

        public List<DayCurve> fearPoolCurve = new();
        public AnimationCurve guestMixCurve;
    }
}