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
                ShopMain = main,
                ShopPrice = price,
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

            // 2) 生成 Pawn（从 World.Shop.Offers 里拿到该槽位真实的 OfferId / Main）
            if (pawnPrefab)
            {
                var game   = FindObjectOfType<Game>();
                var offers = game?.World?.Shop?.Offers;
                GhostOffer offer = null;

                if (offers != null && slotIndex >= 0 && slotIndex < offers.Count)
                    offer = offers[slotIndex];

                var pawn = Instantiate(pawnPrefab, visualRoot);
                pawn.transform.localPosition = pawnLocalOffset;
                pawn.transform.localRotation = Quaternion.identity;
                pawn.transform.localScale    = Vector3.one * Mathf.Max(0.01f, pawnLocalScale);

                // 若能拿到 Offer，用 OfferId 作为临时 Ghost 的 Id（形如 "{cfgId}@offer_day_slot"）
                // PawnView 将按 '@' 前缀解析出 cfgId 并匹配到正确的 GhostConfig。
                if (offer != null && !string.IsNullOrEmpty(offer.OfferId))
                {
                    var fake = new Ghost { Id = offer.OfferId, Main = offer.Main };
                    pawn.BindGhost(fake);
                }
                else
                {
                    // 兜底：保留旧行为，仍可显示一个基于 FearTag 的占位预览
                    var fallbackId = $"preview@offer_{slotIndex}_{main}";
                    var fake = new Ghost { Id = fallbackId, Main = main };
                    pawn.BindGhost(fake);
                }
            }
        }
    }
}
