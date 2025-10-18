using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScreamHotel.Domain;
using ScreamHotel.Data;
using ScreamHotel.Core;
using ScreamHotel.UI;
using Spine.Unity;

namespace ScreamHotel.Presentation
{
    public class PawnView : MonoBehaviour, IHoverInfoProvider
    {
        public string ghostId;

        public Transform replaceRoot;
        
        public void BindGhost(Ghost g)
        {
            ghostId = g.Id;
            name = $"GhostPawn_{g.Id}";
            
            GhostConfig cfg = FindGhostConfig(g);
            
            ApplyConfigAppearance(cfg);

            // 把 id 传给可拖拽组件
            var draggable = GetComponentInParent<DraggablePawn>();
            if (draggable != null) draggable.SetGhostId(ghostId);
        }

        /// <summary>
        /// 解析运行时 Id，按分隔符('#', '@', ':')截取前缀作为配置 id，再到数据库匹配。
        /// 约定：
        ///   - 购买生成："{cfg.id}#shop_..."   （真实实例）
        ///   - 商店预览："{cfg.id}@offer_..." （预览实例）
        /// </summary>
        private GhostConfig FindGhostConfig(Ghost g)
        {
            var game = FindObjectOfType<Game>();
            var db = game != null ? game.dataManager?.Database : null;
            if (db == null) return null;

            var key = g.Id;
            if (!string.IsNullOrEmpty(key))
            {
                int cut = key.IndexOfAny(new[] { '#', '@', ':' });
                if (cut > 0)
                {
                    var baseId = key.Substring(0, cut);
                    if (db.Ghosts.TryGetValue(baseId, out var cfgBase))
                        return cfgBase;
                }

                // 若没有分隔符，就当作完整配置 id 尝试一次
                if (db.Ghosts.TryGetValue(key, out var cfgFull))
                    return cfgFull;
            }

            return null;
        }

        private void ApplyConfigAppearance(GhostConfig cfg)
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
                    var spine = instance.GetComponentInChildren<SkeletonAnimation>(true);
                    if (spine != null)
                    {
                        walker.spineAnim = spine;
                    }
                }
            }
        }
        
        public void MoveTo(Transform target, float dur = 0.5f)
        {
            StopAllCoroutines();
            StartCoroutine(MoveRoutine(target.position, dur));
        }
        
        System.Collections.IEnumerator MoveRoutine(Vector3 to, float dur)
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
            var g = game?.World?.Ghosts?.Find(x => x.Id == ghostId);
            if (g == null)
            {
                Debug.LogWarning($"[PawnView] Ghost not found in World: ghostId={ghostId ?? "<null>"}");
                return list;
            }

            if (TryGetEnum<FearTag>(g, "Main", out var main)) list.Add(main);
            if (TryGetEnum<FearTag>(g, "Sub",  out var sub))  list.Add(sub);
            TryAddList(g, "Tags",  list);
            TryAddList(g, "Fears", list);

            // 更稳的去重：保持原顺序
            var seen = new HashSet<FearTag>();
            list = list.Where(t => seen.Add(t)).ToList();
            return list;
        }
        
        // 1) 读枚举（支持属性或字段）
        static bool TryGetEnum<T>(object obj, string name, out T value) where T : struct
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
        static void TryAddList(object obj, string name, List<FearTag> outList)
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
