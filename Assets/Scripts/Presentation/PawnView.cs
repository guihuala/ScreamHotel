// PawnView.cs
using System.Linq;
using UnityEngine;
using ScreamHotel.Domain;
using ScreamHotel.Data;       // 引入 GhostConfig
using ScreamHotel.Core;       // 便于拿到 Game/DataManager

namespace ScreamHotel.Presentation
{
    public class PawnView : MonoBehaviour
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
    }
}
