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
}
