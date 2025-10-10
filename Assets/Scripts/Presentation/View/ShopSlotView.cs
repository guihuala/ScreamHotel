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

        void Awake()
        {
            if (Application.isPlaying) BuildVisual();
        }

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
            };
        }

        public bool TryClick(Game game)
        {
            if (game == null) return false;
            if (game.ShopTryBuy(slotIndex, out var newGhostId))
            {
                // 1) 本地立刻清掉已实例化的鬼
                if (visualRoot)
                {
                    for (int i = visualRoot.childCount - 1; i >= 0; i--)
                        Destroy(visualRoot.GetChild(i).gameObject);
                }

                // 2) 通知表现层：同步商店（删槽位）+ 同步鬼（把新鬼生到待命/出生区）
                var pc = FindObjectOfType<PresentationController>();
                if (pc)
                {
                    pc.SendMessage("SyncShop",   SendMessageOptions.DontRequireReceiver);
                    pc.SendMessage("SyncGhosts", SendMessageOptions.DontRequireReceiver);
                }
                return true;
            }
            return false;
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
                Destroy(child.gameObject);
            }

            // 2) 生成 Pawn
            if (pawnPrefab)
            {
                var pawn = Instantiate(pawnPrefab, visualRoot);

                pawn.transform.localPosition = pawnLocalOffset;
                pawn.transform.localRotation = Quaternion.identity;
                pawn.transform.localScale    = Vector3.one * Mathf.Max(0.01f, pawnLocalScale);

                var fake = new Ghost { Id = $"Offer_{slotIndex}_{main}", Main = main };
                pawn.BindGhost(fake);
            }
        }
    }
}
