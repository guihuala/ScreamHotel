using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/Progression", fileName = "Progression")]
    public class ProgressionConfig : ScriptableObject
    {
        public AnimationCurve guestMixCurve;
    }
}