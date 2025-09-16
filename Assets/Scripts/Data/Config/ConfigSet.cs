using System.Collections.Generic;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/ConfigSet", fileName = "ConfigSet")]
    public class ConfigSet : ScriptableObject
    {
        [Header("Tables (drag your SO assets here)")]
        public List<GhostConfig> ghosts = new();
        public List<GuestTypeConfig> guestTypes = new();
        public List<RoomPriceConfig> roomPrices = new();

        [Header("Singletons")]
        public ProgressionConfig progression;
        public GameRuleConfig rules;
    }
}