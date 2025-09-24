using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/GameRules", fileName = "GameRules")]
    public class GameRuleConfig : ScriptableObject
    {
        [Header("Day Spawns")]
        public int dayGuestSpawnCount = 4;   // 每天白天刷的客人数
        public int dayGhostSpawnCount = 0;   // 每天白天刷的新鬼数（先设 0）
        
        [Header("Bonus")]
        public float mainBonus = 0.3f;
        public float subBonus = 0.15f;
        public float roomBonus = 0.1f;
    }
}