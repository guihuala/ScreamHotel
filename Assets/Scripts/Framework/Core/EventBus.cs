using System;
using System.Collections.Generic;

namespace ScreamHotel.Core
{
    public interface IGameEvent {}
    
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _subs = new();

        public static void Subscribe<T>(Action<T> cb)
        {
            var t = typeof(T);
            if (!_subs.ContainsKey(t)) _subs[t] = new List<Delegate>();
            _subs[t].Add(cb);
        }

        public static void Unsubscribe<T>(Action<T> cb)
        {
            var t = typeof(T);
            if (_subs.TryGetValue(t, out var list)) list.Remove(cb);
        }

        public static void Raise<T>(T evt)
        {
            if (_subs.TryGetValue(typeof(T), out var list))
                foreach (var d in list.ToArray())
                    (d as Action<T>)?.Invoke(evt);
        }
    }

    
    public readonly struct GameStateChanged
    {
        public readonly object State;

        public GameStateChanged(object s)
        {
            State = s;
        }
    }

    public readonly struct NightResolved
    {
        public readonly object Results;

        public NightResolved(object r)
        {
            Results = r;
        }
    }

    // 经济 / 建造相关事件
    public readonly struct GoldChanged
    {
        public readonly int Gold;

        public GoldChanged(int g)
        {
            Gold = g;
        }
    }

    // 新楼层建成
    public readonly struct FloorBuiltEvent
    {
        public readonly int Floor;
        public FloorBuiltEvent(int floor){ Floor = floor; }
    }
    
    // 屋顶需要更新事件
    public readonly struct RoofUpdateNeeded { }
    
    public readonly struct RoomUnlockedEvent
    {
        public readonly string RoomId;
        public RoomUnlockedEvent(string id) { RoomId = id; }
    }

    
    public readonly struct RoomPurchasedEvent
    {
        public readonly string RoomId;

        public RoomPurchasedEvent(string id)
        {
            RoomId = id;
        }
    }

    public readonly struct RoomUpgradedEvent
    {
        public readonly string RoomId;
        public readonly int NewLevel;

        public RoomUpgradedEvent(string id, int lvl)
        {
            RoomId = id;
            NewLevel = lvl;
        }
    }
    
    public readonly struct DayStartedEvent { }
    
    public readonly struct NightStartedEvent { }
    
    public readonly struct SuspicionChanged
    {
        public readonly int Total;
        public readonly int Delta;
        public SuspicionChanged(int total, int delta) { Total = total; Delta = delta; }
    }

    public readonly struct GameEnded
    {
        public readonly bool Success;
        public readonly string Reason;
        public readonly int DayIndex;
        public GameEnded(bool success, string reason, int dayIndex)
        {
            Success = success; Reason = reason; DayIndex = dayIndex;
        }
    }
}
