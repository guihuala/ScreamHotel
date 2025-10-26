using System.Collections.Generic;

namespace ScreamHotel.Domain
{
    public enum FearTag { Darkness, Blood, Noise, Rot, Gaze }
    public enum GhostState { Idle, Working, Training }
    
    public class Ghost
    {
        public string Id;
        public FearTag Main;
        public FearTag? Sub;
        public GhostState State;
        public int TrainingDays;    // 训练期间的天数
    }


    public class Guest
    {
        public string Id;
        public List<FearTag> Fears;
        public float BarMax;
        public float RequiredPercent;
        public int BaseFee;
        public string TypeId;
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
    
    // ---- 商店状态与货架条目 ----
    public class GhostOffer
    {
        public string OfferId;
        public string ConfigId;
        public FearTag Main;
    }
    
    public class ShopState
    {
        public List<GhostOffer> Offers = new(); // 当天货架（长度==规则槽位数）
        public int DayLastRefreshed = -1;       // 上次刷新是哪一天
    }
    
    public class Economy
    {
        public int Gold;
    }

    public class World
    {
        public int Suspicion = 0; // 全局累计怀疑值
        public readonly List<Ghost> Ghosts = new();
        public readonly List<Guest> Guests = new();
        public readonly List<Room> Rooms = new();
        public readonly Economy Economy = new();
        public readonly Data.ConfigDatabase Config;
        
        public ShopState Shop = new ShopState();
        public World(Data.ConfigDatabase db) { Config = db; }
    }
}
