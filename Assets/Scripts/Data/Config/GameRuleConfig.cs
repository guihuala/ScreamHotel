using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/GameRules", fileName = "GameRules")]
    public class GameRuleConfig : ScriptableObject
    {
        public float mainBonus = 0.3f;
        public float subBonus = 0.15f;
        public float roomBonus = 0.1f;
        public float fatiguePerNight = 0.1f;
        public float fatigueRestRecover = 0.3f;
        public float highFatiguePenalty = 0.2f;
        public int forcedRestDaysOnCounter = 2;
        public float minGoldMultiplier = 0f;
    }
}