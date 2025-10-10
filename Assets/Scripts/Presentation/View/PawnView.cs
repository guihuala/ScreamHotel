using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScreamHotel.Domain;
using ScreamHotel.Data;
using ScreamHotel.Core;
using ScreamHotel.UI;

namespace ScreamHotel.Presentation
{
    public class PawnView : MonoBehaviour, IHoverInfoProvider
    {
        public string ghostId;

        [Header("Render Hooks")]
        public MeshRenderer[] renderers;
        public Transform replaceRoot;

        [Header("Fallback")]
        public MeshRenderer body;

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

        private GhostConfig FindGhostConfig(Ghost g)
        {
            var game = FindObjectOfType<Game>();
            var db   = game != null ? game.dataManager?.Database : null;
            
            GhostConfig cfg = null;
            if (db != null)
            {
                // 按 id 精确匹配
                if (db.Ghosts != null && db.Ghosts.TryGetValue(g.Id, out var byId)) cfg = byId;

                // 兜底：按主 FearTag 找第一条（当 Ghost 的 id 不等于配置 id 时）
                if (cfg == null && db.Ghosts != null)
                    cfg = db.Ghosts.Values.FirstOrDefault(x => x != null && x.main == g.Main);
            }
            return cfg;
        }

        private bool ApplyConfigAppearance(GhostConfig cfg)
        {
            if (cfg == null) return false;
            
            if (cfg.prefabOverride != null && replaceRoot != null)
            {
                for (int i = replaceRoot.childCount - 1; i >= 0; i--)
                    Destroy(replaceRoot.GetChild(i).gameObject);
                Instantiate(cfg.prefabOverride, replaceRoot);
            }
            
            var targets = (renderers != null && renderers.Length > 0)
                            ? renderers
                            : (body != null ? new[] { body } : null);

            if (targets != null)
            {
                foreach (var r in targets)
                {
                    if (!r) continue;

                    if (cfg.overrideMaterial != null)
                        r.material = cfg.overrideMaterial;

                    if (cfg.colorTint.HasValue && r.material.HasProperty("_Color"))
                        r.material.color = cfg.colorTint.Value;

                    if (cfg.colorTint.HasValue && r.material.HasProperty("_EmissionColor"))
                    {
                        r.material.EnableKeyword("_EMISSION");
                        r.material.SetColor("_EmissionColor", cfg.colorTint.Value * 0f); // 需要发光可加系数
                    }
                }
            }

            // 只要配置里有任一外观项，就认为已应用成功
            return cfg.overrideMaterial || cfg.colorTint.HasValue || cfg.prefabOverride;
        }
        
        public void SnapTo(Vector3 target) { transform.position = target; }
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
        
        public GameObject BuildVisualPreview(string layerName = "Ignore Raycast")
        {
            var previewRoot = new GameObject($"{name}_Preview");

            Transform visualRoot = (replaceRoot != null && replaceRoot.childCount > 0) ? replaceRoot : transform;

            // 1) 预览根对齐到可视根的【世界变换】
            previewRoot.transform.position = visualRoot.position;
            previewRoot.transform.rotation = visualRoot.rotation;
            previewRoot.transform.localScale = visualRoot.lossyScale;

            int lyr = LayerMask.NameToLayer(layerName);
            if (lyr >= 0) SetLayerRecursively(previewRoot, lyr);

            // 2) 克隆 replaceRoot 子物体（保留世界姿态）
            if (replaceRoot != null && replaceRoot.childCount > 0)
            {
                for (int i = 0; i < replaceRoot.childCount; i++)
                {
                    var child = replaceRoot.GetChild(i).gameObject;
                    var clone = Instantiate(child, previewRoot.transform, true); // 保留世界姿态
                    SanitizePreviewNode(clone);
                }
                return previewRoot;
            }

            // 否则按 renderers/body 克
            var targets = (renderers != null && renderers.Length > 0)
                ? renderers
                : (body != null ? new[] { body } : null);

            if (targets != null)
            {
                foreach (var r in targets)
                {
                    if (!r) continue;
                    var clone = Instantiate(r.gameObject, previewRoot.transform, true); // 保留世界姿态
                    SanitizePreviewNode(clone);
                }
            }

            return previewRoot;
        }

        private static void SanitizePreviewNode(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);

            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is Animator) continue;
                Destroy(mb);
            }
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
        }
        
        public List<FearTag> GetFearTags()
        {
            var list = new List<FearTag>();
            var game = FindObjectOfType<Game>();
            var g = game?.World?.Ghosts?.Find(x => x.Id == ghostId);
            if (g == null) return list;

            // 常见：Ghost.Main / Ghost.Sub；再兼容 Tags/Fears 集合
            if (TryGetEnum<FearTag>(g, "Main", out var main)) list.Add(main);
            if (TryGetEnum<FearTag>(g, "Sub", out var sub))   list.Add(sub);
            TryAddList(g, "Tags", list);
            TryAddList(g, "Fears", list);

            // 去重
            for (int i = list.Count - 1; i >= 0; --i)
                if (i > 0 && list.GetRange(0, i).Contains(list[i])) list.RemoveAt(i);

            return list;
        }
        
        static bool TryGetEnum<T>(object obj, string prop, out T value) where T : struct
        {
            value = default;
            var p = obj.GetType().GetProperty(prop);
            if (p != null && p.PropertyType.IsEnum)
            {
                var v = p.GetValue(obj);
                if (v != null) { value = (T)v; return true; }
            }
            return false;
        }
        static void TryAddList(object obj, string prop, List<FearTag> outList)
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
