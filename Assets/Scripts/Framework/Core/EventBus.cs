using System;
using System.Collections.Generic;

namespace ScreamHotel.Core
{
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

    public readonly struct GameStateChanged { public readonly object State; public GameStateChanged(object s){ State = s; } }
    public readonly struct NightResolved { public readonly object Results; public NightResolved(object r){ Results = r; } }
}
