using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.UI;

namespace ScreamHotel.Presentation
{
    public class ShopSlotView : MonoBehaviour, IHoverInfoProvider, IClickActionProvider
    {
        [Header("Bind")]
        public int slotIndex = -1;
        public FearTag main;

        [Header("Visual Roots")]
        [Tooltip("所有可视元素的父节点。")]
        public Transform visualRoot;

        [Header("Prefabs")]
        [Tooltip("鬼的展示用 PawnView 预制件（简模即可）。")]
        public PawnView pawnPrefab;

        [Header("Layout")]
        public Vector3 pawnLocalOffset = new Vector3(0, 0.5f, 0);
        public float   pawnLocalScale  = 1.0f;

        [Header("Hover UI")]
        [Tooltip("鼠标悬停时面板的屏幕像素偏移（相对于鼠标位置）。")]
        public Vector2 hoverScreenOffset = new Vector2(20f, 16f);

        void Awake()
        {
            // 仅在运行时构建，避免编辑器态频繁改 Inspector 时反复实例化
            if (Application.isPlaying) BuildVisual();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // 在编辑器静态预览时也允许重建，但一定要清理旧子物体
            if (!Application.isPlaying) BuildVisual();
        }
#endif

        public HoverInfo GetHoverInfo()
        {
            var game = FindObjectOfType<Game>();
            int price = game?.World?.Config?.Rules?.ghostShopPrice ?? 0;
            return new HoverInfo
            {
                Kind = HoverKind.ShopSlot,
                ShopSlotIndex = slotIndex,
                ShopMain = main,
                ShopPrice = price,
                WorldPosition = transform.position,
                ScreenOffset = hoverScreenOffset
            };
        }

        public bool TryClick(Game game)
        {
            return game != null && game.ShopTryBuy(slotIndex, out _);
        }

        public void Rebind(FearTag newMain, int newIndex)
        {
            main = newMain;
            slotIndex = newIndex;
            BuildVisual();
        }

        private void BuildVisual()
        {
            if (!visualRoot) return;

            // 1) 彻底清空旧可视
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                var child = visualRoot.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.DestroyObjectImmediate(child.gameObject); // 更友好：可撤销
                else
                    Destroy(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }

            // 2) 生成 Pawn（用 Fake Ghost 只为着色/命名）
            if (pawnPrefab)
            {
#if UNITY_EDITOR
                PawnView pawn;
                if (!Application.isPlaying)
                {
                    var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(pawnPrefab.gameObject, visualRoot);
                    pawn = go.GetComponent<PawnView>();
                }
                else
                {
                    pawn = Instantiate(pawnPrefab, visualRoot);
                }
#else
                var pawn = Instantiate(pawnPrefab, visualRoot);
#endif
                pawn.transform.localPosition = pawnLocalOffset;
                pawn.transform.localRotation = Quaternion.identity;
                pawn.transform.localScale    = Vector3.one * Mathf.Max(0.01f, pawnLocalScale);

                var fake = new Ghost { Id = $"Offer_{slotIndex}_{main}", Main = main };
                pawn.BindGhost(fake);
            }
        }
    }
}
