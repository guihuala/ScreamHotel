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

            if (!string.IsNullOrEmpty(g.TypeId) && db.GuestTypes.TryGetValue(g.TypeId, out var cfg))
                return cfg;
            
            return null;
        }

        // ====== Apply Appearance） ======
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
        
        public GameObject BuildVisualPreview(string layerName = "Ignore Raycast")
        {
            var previewRoot = new GameObject($"{name}_Preview");

            // 选一个“可视根”做对齐基准：优先 replaceRoot，否则用当前 GuestView 的 transform
            Transform visualRoot = (replaceRoot != null && replaceRoot.childCount > 0) ? replaceRoot : transform;

            // 1) 预览根对齐到可视根的【世界变换】
            previewRoot.transform.position = visualRoot.position;
            previewRoot.transform.rotation = visualRoot.rotation;
            // 预览根在世界根下，所以直接使用可视根的 lossyScale 作为预览根的 localScale
            previewRoot.transform.localScale = visualRoot.lossyScale;

            // 设层，避免点击
            int lyr = LayerMask.NameToLayer(layerName);
            if (lyr >= 0) SetLayerRecursively(previewRoot, lyr);

            // 2) 克隆：如果有 replaceRoot 的子物体，就把它们按【世界变换】克到预览根下
            if (replaceRoot != null && replaceRoot.childCount > 0)
            {
                for (int i = 0; i < replaceRoot.childCount; i++)
                {
                    var child = replaceRoot.GetChild(i).gameObject;
                    // instantiateInWorldSpace = true 以保留世界姿态
                    var clone = Instantiate(child, previewRoot.transform, true);
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

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
        }
    }
}