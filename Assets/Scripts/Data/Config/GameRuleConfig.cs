using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/GameRules", fileName = "GameRules")]
    public class GameRuleConfig : ScriptableObject
    {
        [Header("Suspicion & Ending")] [Tooltip("怀疑值上限，达到或超过时立即触发失败结局")]
        public int suspicionThreshold = 100;
        [Tooltip("每位【吓人失败】的已接待客人增加的怀疑值")] public int suspicionPerFailedGuest = 1;
        [Tooltip("本局总天数（坚持到最后一天且未满怀疑值 = 成功结局）")]
        public int totalDays = 10;
        
        [Header("Ending – Gold Target")]
        [Tooltip("是否启用：到最后一天结算时，必须达到目标金币才算成功")]
        public bool requireTargetGold = false;
        [Tooltip("成功所需的目标金币（仅在启用时生效）")]
        public int targetGold = 0;
        
        [Header("Day Spawns")]
        [Tooltip("每天白天刷新的客人数")]
        public int dayGuestSpawnCount = 4;
        
        [Header("Ghost Shop")]
        [Tooltip("鬼魂商店的槽位数量")] public int ghostShopSlots = 5;
        [Tooltip("鬼魂的统一售价")] public int ghostShopPrice = 100;
        [Tooltip("刷新商店所需的消耗")] public int ghostShopRerollCost = 50;
        [Tooltip("是否在同一轮商店中不重复出现相同的主恐惧类型")] public bool ghostShopUniqueMains = true;

        [Header("Build Prices")] 
        public int floorBuildBaseCost = 500;
        public float floorCostGrowth = 1.25f;
        [Tooltip("房间升级的价格：Index 0 表示 Lv1→Lv2，Index 1 表示 Lv2→Lv3")] 
        public int[] roomUpgradeCosts = new[] { 200, 400 };

        [Tooltip("可建造的最高楼层上限")]
        public int maxFloor = 4;

        [Header("Room Pricing & Capacity")]
        [Tooltip("解锁一间房间（Lv0→Lv1）的价格")] public int roomUnlockCost = 150;
        [Tooltip("Lv1与Lv3的容量（Lv2若与Lv1一致则无需字段）")] public int capacityLv1 = 1;
        public int capacityLv3 = 2;
        [Tooltip("Lv2/3 是否赋予房间恐惧Tag（装饰表现用）")] public bool lv2HasTag = true;
        
        [Header("Ghost Training")]
        public int ghostTrainingTimeDays = 2; // 统一的训练天数（每个鬼怪都相同）

        [Header("Time Configuration")]
        [Tooltip("一天的总时长（单位：秒）")] public float dayDurationInSeconds = 300f; // 默认为 5 分钟
        [Tooltip("一天开始时的时间（0 到 1 之间的值，0 为午夜，1 为第二天的午夜）")] public float dayStartTime = 0f;  // 默认为 0（午夜）
        
        [Tooltip("白天所占比例")]      public float dayRatio = 0.50f;
        [Tooltip("夜间展示所占比例")]  public float nightShowRatio = 0.20f;
        [Tooltip("夜间执行所占比例")]  public float nightExecuteRatio = 0.20f;
        [Tooltip("结算所占比例")]      public float settlementRatio = 0.10f;
    }
}