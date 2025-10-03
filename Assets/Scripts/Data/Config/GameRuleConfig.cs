using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/GameRules", fileName = "GameRules")]
    public class GameRuleConfig : ScriptableObject
    {
        [Header("Day Spawns")]
        [Tooltip("每天白天刷新的客人数")]
        public int dayGuestSpawnCount = 4;
        
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

        [Header("Build Prices")] 
        public int floorBuildBaseCost = 500;
        public float floorCostGrowth = 1.25f;
        [Tooltip("房间升级的价格：Index 0 表示 Lv1→Lv2，Index 1 表示 Lv2→Lv3")] public int[] roomUpgradeCosts = new[] { 200, 400 };

        [Header("Room Pricing & Capacity")]
        [Tooltip("购买一间Lv1房间的价格")] public int roomBuyCost = 200;
        [Tooltip("Lv1与Lv3的容量（Lv2若与Lv1一致则无需字段）")] public int capacityLv1 = 1;
        public int capacityLv3 = 2;
        [Tooltip("Lv2/3 是否赋予房间恐惧Tag（装饰表现用）")] public bool lv2HasTag = true;
        public bool lv3HasTag = true;
    }
}