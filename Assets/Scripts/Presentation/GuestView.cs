using System.Linq;
using UnityEngine;
using ScreamHotel.Domain;
using ScreamHotel.Core;
using ScreamHotel.Data;

namespace ScreamHotel.Presentation
{
    public class GuestView : MonoBehaviour
    {
        [Header("Identity")]
        public string guestId;

        [Header("Render Hooks")]
        public MeshRenderer[] renderers;
        public Transform replaceRoot;

        [Header("Fallback (Optional)")]
        public MeshRenderer body;

        // ====== Bind ======
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

            // 以 TypeId 精准命中（你的数据库里 GuestTypes 用 id 作为 key）
            if (!string.IsNullOrEmpty(g.TypeId) && db.GuestTypes.TryGetValue(g.TypeId, out var cfg))
                return cfg;

            // 兜底：如果没有 TypeId（老存档/测试），可以按你自定义逻辑去找
            return null;
        }

        // ====== Apply Appearance（与 PawnView 对齐的实现方式） ======
        private void ApplyConfigAppearance(GuestTypeConfig cfg)
        {
            if (cfg == null)
            {
                // 没配置就不强行改材质/颜色，保持 prefab 原样
                return;
            }

            // 1) Prefab 替换（可选）
            if (cfg.prefabOverride != null && replaceRoot != null)
            {
                for (int i = replaceRoot.childCount - 1; i >= 0; i--)
                    Destroy(replaceRoot.GetChild(i).gameObject);
                Instantiate(cfg.prefabOverride, replaceRoot);
            }

            // 2) 渲染器上色/材质替换
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
                    else
                        r.material.color = cfg.colorTint;  // GuestTypeConfig 的颜色字段

                    if (r.material.HasProperty("_EmissionColor"))
                    {
                        // 如需发光，按需求添加系数
                        // r.material.EnableKeyword("_EMISSION");
                        // r.material.SetColor("_EmissionColor", r.material.color * emissionFactor);
                    }
                }
            }

            // 3) 你还有 icon 等 UI 字段，可在需要处使用
        }

        // ====== Move（与 PawnView 对齐） ======
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
    }
}