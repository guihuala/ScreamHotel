using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/GameRules", fileName = "GameRules")]
    public class GameRuleConfig : ScriptableObject
    {
        [Header("Day Spawns")] 
        [Tooltip("每天白天刷新的客人数")] public int dayGuestSpawnCount = 4;
        [Tooltip("每天白天刷新的新鬼数（建议先设为0）")] public int dayGhostSpawnCount = 0;

        [Header("Bonus")] 
        [Tooltip("主恐惧类型提供的加成系数")] public float mainBonus = 0.3f;
        [Tooltip("副恐惧类型提供的加成系数")] public float subBonus = 0.15f;
        [Tooltip("房间本身提供的加成系数")] public float roomBonus = 0.1f;

        [Header("Ghost Shop")]
        [Tooltip("鬼魂商店的槽位数量")] public int ghostShopSlots = 5;
        [Tooltip("鬼魂的统一售价")] public int ghostShopPrice = 100;
        [Tooltip("刷新商店所需的消耗")] public int ghostShopRerollCost = 50;
        [Tooltip("是否在同一轮商店中不重复出现相同的主恐惧类型")] public bool ghostShopUniqueMains = true;
        [Tooltip("购买鬼魂后加入世界时提供的基础恐惧值")] public float shopSpawnBaseScare = 10f;
    }
}