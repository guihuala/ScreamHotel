using System.Collections.Generic;
using UnityEngine;
using ScreamHotel.Domain;

namespace ScreamHotel.Data
{
    public class ConfigDatabase
    {
        public readonly Dictionary<string, GhostConfig> Ghosts = new();
        public readonly Dictionary<string, GuestTypeConfig> GuestTypes = new();
        public readonly Dictionary<string, RoomPriceConfig> RoomPrices = new();
        public ProgressionConfig Progression;
        public GameRuleConfig Rules;

        public override string ToString() => $"Ghosts:{Ghosts.Count}, Guests:{GuestTypes.Count}, Rooms:{RoomPrices.Count}";

        public GhostConfig GetGhost(string id) => Ghosts[id];
        public GuestTypeConfig GetGuestType(string id) => GuestTypes[id];
        public RoomPriceConfig GetRoomPrice(string id) => RoomPrices[id];
    }

    [CreateAssetMenu(menuName = "ScreamHotel/Ghost", fileName = "Ghost__")]
    public class GhostConfig : ScriptableObject
    {
        public string id;
        public FearTag main;
        public FearTag subRestriction;
        public float baseScare = 30f;
        public int recruitCost = 100;
    }

    [CreateAssetMenu(menuName = "ScreamHotel/GuestType", fileName = "GuestType__")]
    public class GuestTypeConfig : ScriptableObject
    {
        public string id;
        public List<FearTag> fears;
        public float barMax = 100f;
        [Range(0.5f, 0.99f)] public float requiredPercent = 0.8f;
        public int baseFee = 100;
        [Header("Counter Rules")] public float counterChance = 0f;
    }

    [CreateAssetMenu(menuName = "ScreamHotel/RoomPrice", fileName = "RoomPrice__")]
    public class RoomPriceConfig : ScriptableObject
    {
        public string id;
        public int buyCost = 200;
        public int upgradeToLv2 = 150;
        public int upgradeToLv3 = 300;
        public int capacityLv1 = 1;
        public int capacityLv3 = 2;
        public bool lv2HasTag = true;
        public bool lv3HasTag = true;
    }

    [CreateAssetMenu(menuName = "ScreamHotel/Progression", fileName = "Progression")]
    public class ProgressionConfig : ScriptableObject
    {
        [System.Serializable] public struct DayCurve { public int day; public int availableFearCount; }
        public List<DayCurve> fearPoolCurve = new();
        public AnimationCurve guestMixCurve;
    }

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
