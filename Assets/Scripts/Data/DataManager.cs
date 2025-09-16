using System;
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

                foreach (var rp in configSet.roomPrices)
                    if (rp != null && !string.IsNullOrEmpty(rp.id))
                        Database.RoomPrices[rp.id] = rp;

                if (configSet.progression != null) Database.Progression = configSet.progression;
                if (configSet.rules != null)       Database.Rules       = configSet.rules;
            }
            
            LogSummary();
        }
        
        private void LogSummary()
        {
            Debug.Log(
                $"[DataManager] Loaded Summary (Direct SO={configSet!=null}): " +
                $"Ghosts:{Database.Ghosts.Count}, Guests:{Database.GuestTypes.Count}, Rooms:{Database.RoomPrices.Count}, " +
                $"Progression:{(Database.Progression? "Y":"N")}, Rules:{(Database.Rules? "Y":"N")}"
            );
        }
    }
}
