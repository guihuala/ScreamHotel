using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScreamHotel.Domain;
using ScreamHotel.Core;
using ScreamHotel.Data;
using ScreamHotel.UI;
using Spine.Unity;

namespace ScreamHotel.Presentation
{
    public class GuestView : MonoBehaviour, IHoverInfoProvider
    {
        [Header("Identity")]
        public string guestId;
        public Transform replaceRoot;
        
        // 显式按 Id 绑定
        public void BindGuest(string id)
        {
            guestId = id;

            var game = FindObjectOfType<Game>();
            var g = game?.World?.Guests?.Find(x => x.Id == guestId);
            if (g == null)
            {
                Debug.LogWarning($"[GuestView] Guest not found: {guestId}");
                return;
            }
            Bind(g);
        }

        // 按 Domain.Guest 绑定
        public void Bind(Guest g)
        {
            guestId = g.Id;
            name = $"Guest_{g.Id}";

            var cfg = FindGuestTypeConfig(g);
            ApplyConfigAppearance(cfg);

            // 把 id 传给可拖拽组件
            var draggable = GetComponentInParent<DraggableGuest>();
            if (draggable != null) draggable.SetGuestId(guestId);
        }
        
        private GuestTypeConfig FindGuestTypeConfig(Guest g)
        {
            var game = FindObjectOfType<Game>();
            var db = game != null ? game.dataManager?.Database : null;
            if (db == null) return null;

            if (!string.IsNullOrEmpty(g.TypeId) && db.GuestTypes.TryGetValue(g.TypeId, out var cfg))
                return cfg;
            
            return null;
        }
        
        private void ApplyConfigAppearance(GuestTypeConfig cfg)
        {
            if (cfg == null) return;

            if (cfg.prefabOverride != null && replaceRoot != null)
            {
                // 清空旧的
                for (int i = replaceRoot.childCount - 1; i >= 0; i--)
                    Destroy(replaceRoot.GetChild(i).gameObject);

                // 实例化新的 prefab
                GameObject instance = Instantiate(cfg.prefabOverride, replaceRoot);
            }
        }
        
        // ====== Move======
        public void MoveTo(Transform target, float dur = 0.5f)
        {
            StopAllCoroutines();
            StartCoroutine(MoveRoutine(target.position, dur));
        }

        private System.Collections.IEnumerator MoveRoutine(Vector3 to, float dur)
        {
            var from = transform.position; float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, dur);
                transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            transform.position = to;
        }
        
        public List<FearTag> GetFearTags()
        {
            var list = new List<FearTag>();
            var game = FindObjectOfType<Game>();
            var g = game?.World?.Guests?.Find(x => x.Id == guestId);
            if (g == null) return list;

            TryGetEnumByMember<FearTag>(g, "Main", out var main);     // 若以后给 Guest 也加 Main/Sub 可自动兼容
            TryGetEnumByMember<FearTag>(g, "Sub", out var sub);
            if (!EqualityComparer<FearTag>.Default.Equals(main, default)) list.Add(main);
            if (!EqualityComparer<FearTag>.Default.Equals(sub,  default)) list.Add(sub);

            TryAddListByMember(g, "Tags",  list);
            TryAddListByMember(g, "Fears", list);

            // 去重保持顺序
            var seen = new HashSet<FearTag>();
            list = list.Where(t => seen.Add(t)).ToList();
            return list;
        }
        
        // 1) 读枚举（支持属性或字段）
        static bool TryGetEnumByMember<T>(object obj, string name, out T value) where T : struct
        {
            value = default;

            // 先找属性
            var p = obj.GetType().GetProperty(name);
            if (p != null)
            {
                var t = p.PropertyType;
                var underlying = System.Nullable.GetUnderlyingType(t);
                bool isEnumOrNullableEnum = t.IsEnum || (underlying != null && underlying.IsEnum);
                if (!isEnumOrNullableEnum) return false;

                var v = p.GetValue(obj);
                if (v == null) return false;
                if (underlying != null) v = System.Convert.ChangeType(v, underlying);
                value = (T)v;
                return true;
            }

            // 再找字段
            var f = obj.GetType().GetField(name);
            if (f != null)
            {
                var t = f.FieldType;
                var underlying = System.Nullable.GetUnderlyingType(t);
                bool isEnumOrNullableEnum = t.IsEnum || (underlying != null && underlying.IsEnum);
                if (!isEnumOrNullableEnum) return false;

                var v = f.GetValue(obj);
                if (v == null) return false;
                if (underlying != null) v = System.Convert.ChangeType(v, underlying);
                value = (T)v;
                return true;
            }

            return false;
        }

        // 2) 读列表（支持属性或字段）
        static void TryAddListByMember(object obj, string name, List<FearTag> outList)
        {
            // 属性
            var p = obj.GetType().GetProperty(name);
            if (p != null && typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType))
            {
                var en = (System.Collections.IEnumerable)p.GetValue(obj);
                if (en != null) foreach (var x in en) if (x is FearTag t) outList.Add(t);
                return;
            }

            // 字段
            var f = obj.GetType().GetField(name);
            if (f != null && typeof(System.Collections.IEnumerable).IsAssignableFrom(f.FieldType))
            {
                var en = (System.Collections.IEnumerable)f.GetValue(obj);
                if (en != null) foreach (var x in en) if (x is FearTag t) outList.Add(t);
            }
        }

        
        public HoverInfo GetHoverInfo() => new HoverInfo { Kind = HoverKind.Character };
    }
}