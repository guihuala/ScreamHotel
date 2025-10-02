using System.Collections.Generic;

namespace ScreamHotel.Domain
{
    public enum FearTag { Darkness, Blood, Noise, Rot, Gaze }
    public enum GhostState { Idle, Working, Training }
    public enum GuestType { Coward, WhiteCollar, NightOwl, Skeptic, Exorcist }

    public class Ghost
    {
        public string Id;
        public FearTag Main;
        public FearTag? Sub;
        public GhostState State;
        public int DaysForcedRest;
    }

    public class Guest
    {
        public string Id;
        public List<FearTag> Fears;
        public float BarMax;
        public float RequiredPercent;
        public int BaseFee;
        public GuestType Type;
    }

    public class Room
    {
        public string Id;
        public int Level;
        public FearTag? RoomTag;
        public int Capacity;
        public readonly List<string> AssignedGhostIds = new();
        public List<string> AssignedGuestIds = new(); 
    }

    public class Basement
    {
     
        public int TrainingSlots;
    }
    
    // ---- 商店状态与货架条目 ----
    public class GhostOffer
    {
        public string OfferId;     // "Offer_第几天_序号"
        public FearTag Main;       // 鬼的主恐惧，作为“品种”
    }
    
    public class ShopState
    {
        public List<GhostOffer> Offers = new(); // 当天货架（长度==规则槽位数）
        public int DayLastRefreshed = -1;       // 上次刷新是哪一天
    }


    public class Economy
    {
        public int Gold;
        public int RoomPriceTier;
    }

    public class World
    {
        public readonly List<Ghost> Ghosts = new();
        public readonly List<Guest> Guests = new();
        public readonly List<Room> Rooms = new();
        public readonly Basement Basement = new();
        public readonly Economy Economy = new();
        public readonly Data.ConfigDatabase Config;
        
        public ShopState Shop = new ShopState();
        public World(Data.ConfigDatabase db) { Config = db; }
    }
}
