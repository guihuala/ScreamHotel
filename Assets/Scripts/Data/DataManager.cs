using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScreamHotel.Data
{
    public class DataManager : MonoBehaviour
    {
        [Header("SO")]
        public ConfigSet configSet;
        public ConfigDatabase Database { get; private set; } = new();

        public void Initialize()
        {
            Database = new ConfigDatabase();

            if (configSet != null)
            {
                // 直接读so
                foreach (var g in configSet.ghosts)
                    if (g != null && !string.IsNullOrEmpty(g.id))
                        Database.Ghosts[g.id] = g;

                foreach (var gt in configSet.guestTypes)
                    if (gt != null && !string.IsNullOrEmpty(gt.id))
                        Database.GuestTypes[gt.id] = gt;

                if (configSet.progression != null) Database.Progression = configSet.progression;
                if (configSet.rules != null) Database.Rules = configSet.rules;
            }
        }
    }
    
    public class ConfigDatabase
    {
        public readonly Dictionary<string, GhostConfig> Ghosts = new();
        public readonly Dictionary<string, GuestTypeConfig> GuestTypes = new();
        public ProgressionConfig Progression;
        public GameRuleConfig Rules;
        public override string ToString() => $"Ghosts:{Ghosts.Count}, Guests:{GuestTypes.Count}";
        public GhostConfig GetGhost(string id) => Ghosts[id];
        public GuestTypeConfig GetGuestType(string id) => GuestTypes[id];
    }
}
