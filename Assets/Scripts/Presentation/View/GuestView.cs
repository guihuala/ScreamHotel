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

        // ====== Config Lookup ======
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
                
                var walker = GetComponentInChildren<PatrolWalker>(true);
                if (walker != null)
                {
                    // 从新实例里取 SkeletonAnimation
                    var spine = instance.GetComponentInChildren<SkeletonAnimation>(true);
                    if (spine != null)
                    {
                        walker.spineAnim = spine;
                    }
                }
            }
        }
        
        // ====== Move======
        public void SnapTo(Vector3 pos) { transform.position = pos; }

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

        private static void SanitizePreviewNode(GameObject go)
        {
            // 禁用碰撞与刚体
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);

            // 删除所有非渲染/动画的脚本，避免逻辑运行
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is Animator) continue;
                Destroy(mb);
            }
        }
        
        public List<FearTag> GetFearTags()
        {
            var list = new List<FearTag>();
            var game = FindObjectOfType<Game>();
            var g = game?.World?.Guests?.Find(x => x.Id == guestId);
            if (g == null) return list;

            // 常见做法：Guest 可能有 Main/Sub/Tags；按需采集
            TryAddTagByProperty(g, "Main", list);
            TryAddTagByProperty(g, "Sub", list);
            TryAddListByProperty(g, "Tags", list);
            TryAddListByProperty(g, "Fears", list); // 兼容命名

            // 去重
            for (int i = list.Count - 1; i >= 0; --i)
                if (i > 0 && list.GetRange(0, i).Contains(list[i])) list.RemoveAt(i);

            return list;
        }
        
        static void TryAddTagByProperty(object obj, string prop, List<FearTag> outList)
        {
            var p = obj.GetType().GetProperty(prop);
            if (p != null && p.PropertyType.IsEnum)
            {
                var v = p.GetValue(obj);
                if (v != null) outList.Add((FearTag)v);
            }
        }
        static void TryAddListByProperty(object obj, string prop, List<FearTag> outList)
        {
            var p = obj.GetType().GetProperty(prop);
            if (p != null && typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType))
            {
                var en = (System.Collections.IEnumerable)p.GetValue(obj);
                if (en == null) return;
                foreach (var x in en) if (x is FearTag t) outList.Add(t);
            }
        }
        
        public HoverInfo GetHoverInfo() => new HoverInfo { Kind = HoverKind.Character };
    }
}