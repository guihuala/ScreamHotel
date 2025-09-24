using System.Collections.Generic;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/ConfigSet", fileName = "ConfigSet")]
    public class ConfigSet : ScriptableObject
    {
        [Header("Tables")]
        public List<GhostConfig> ghosts = new();
        public List<GuestTypeConfig> guestTypes = new();
        public List<RoomPriceConfig> roomPrices = new();

        [Header("Singletons")]
        public ProgressionConfig progression;
        public GameRuleConfig rules;
    }
}