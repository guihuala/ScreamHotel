using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;

namespace ScreamHotel.Utils
{
    /// <summary>
    /// 统一解析 Guest/Ghost 的恐惧标签（FearTag）。
    /// - 兼容：属性 or 字段；大小写不敏感；单值(Main/Sub)与集合(Tags/Fears/FearTags)。
    /// - 提供：FromGhost/FromGuest 入口 + 调试开关与成员 Dump。
    /// </summary>
    public static class FearTagUtils
    {
        static readonly BindingFlags FLAGS =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

        // 可按需扩展别名
        static readonly string[] MAIN_NAMES = { "Main", "MainTag" };
        static readonly string[] SUB_NAMES  = { "Sub",  "SubTag"  };
        static readonly string[] LIST_NAMES = { "Tags", "Fears", "FearTags" };

        public static List<FearTag> FromGhost(Game game, string ghostId,
            bool debug = false, bool dumpWhenEmpty = true, UnityEngine.Object ctx = null)
        {
            var ghost = game?.World?.Ghosts?.Find(x => x.Id == ghostId);
            if (ghost == null)
            {
                if (debug) Debug.LogWarning($"[FearTagUtils] Ghost not found: id={ghostId}", ctx);
                return new List<FearTag>();
            }
            return CollectFrom(ghost, debug, dumpWhenEmpty, ctx);
        }

        public static List<FearTag> FromGuest(Game game, string guestId,
            bool debug = false, bool dumpWhenEmpty = true, UnityEngine.Object ctx = null)
        {
            var guest = game?.World?.Guests?.Find(x => x.Id == guestId);
            if (guest == null)
            {
                if (debug) Debug.LogWarning($"[FearTagUtils] Guest not found: id={guestId}", ctx);
                return new List<FearTag>();
            }
            return CollectFrom(guest, debug, dumpWhenEmpty, ctx);
        }

        /// <summary>
        /// 通用对象解析
        /// </summary>
        public static List<FearTag> CollectFrom(object obj,
            bool debug = false, bool dumpWhenEmpty = true, UnityEngine.Object ctx = null)
        {
            var list = new List<FearTag>();
            if (obj == null) return list;

            // 单值：Main/Sub（属性或字段、大小写不敏感）
            foreach (var n in MAIN_NAMES) TryAddEnumByMember(obj, n, list);
            foreach (var n in SUB_NAMES ) TryAddEnumByMember(obj, n, list);

            // 集合：Tags/Fears/FearTags
            foreach (var n in LIST_NAMES) TryAddEnumListByMember(obj, n, list);

            // 去重 & 去掉 default(0)（如果 0 表示“无”）
            var seen = new HashSet<FearTag>();
            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (EqualityComparer<FearTag>.Default.Equals(list[i], default) || !seen.Add(list[i]))
                    list.RemoveAt(i);
            }

            if (debug)
                Debug.Log($"[FearTagUtils] Parsed: {string.Join(",", list)}  (type={obj.GetType().Name})", ctx);

            if (list.Count == 0 && dumpWhenEmpty)
                DumpMembers(obj, "[FearTagUtils] Dump Members");

            return list;
        }

        // ---------- 反射辅助 ----------
        static bool TryAddEnumByMember<TEnum>(object obj, string name, List<TEnum> outList) where TEnum : struct
        {
            var t = obj.GetType();

            // Property
            var p = t.GetProperty(name, FLAGS);
            if (p != null && TryCoerceEnum(p.GetValue(obj), out TEnum e1))
            {
                outList.Add(e1);
                return true;
            }

            // Field
            var f = t.GetField(name, FLAGS);
            if (f != null && TryCoerceEnum(f.GetValue(obj), out TEnum e2))
            {
                outList.Add(e2);
                return true;
            }

            return false;
        }

        static bool TryAddEnumListByMember<TEnum>(object obj, string name, List<TEnum> outList) where TEnum : struct
        {
            var t = obj.GetType();
            object val = null;

            var p = t.GetProperty(name, FLAGS);
            if (p != null) val = p.GetValue(obj);
            else
            {
                var f = t.GetField(name, FLAGS);
                if (f != null) val = f.GetValue(obj);
            }
            if (val == null) return false;

            var any = false;
            if (val is System.Collections.IEnumerable en)
            {
                foreach (var x in en)
                    if (TryCoerceEnum(x, out TEnum e)) { outList.Add(e); any = true; }
            }
            return any;
        }

        static bool TryCoerceEnum<TEnum>(object v, out TEnum e) where TEnum : struct
        {
            e = default;
            if (v == null) return false;

            if (v is TEnum ee) { e = ee; return true; }

            // 允许字符串名
            if (v is string s && Enum.TryParse<TEnum>(s, true, out var parsed))
            {
                e = parsed; return true;
            }
            return false;
        }

        public static void DumpMembers(object obj, string prefix)
        {
            if (obj == null) return;
            var t = obj.GetType();
            var props = t.GetProperties(FLAGS).Select(p => $"{p.Name}:{p.PropertyType.Name}");
            var fields = t.GetFields(FLAGS).Select(f => $"{f.Name}:{f.FieldType.Name}");
            Debug.Log($"{prefix} {t.Name}\nPROPS: {string.Join(", ", props)}\nFIELDS: {string.Join(", ", fields)}");
        }
    }
}
