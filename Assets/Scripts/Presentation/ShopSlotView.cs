using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.UI;

namespace ScreamHotel.Presentation
{
    public class ShopSlotView : MonoBehaviour, IHoverInfoProvider, IClickActionProvider
    {
        public int slotIndex = -1;
        public FearTag main;
        public Transform visualRoot; // 挂图标/简模的节点

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
                WorldPosition = transform.position
            };
        }

        public bool TryClick(Game game)
        {
            return game != null && game.ShopTryBuy(slotIndex, out _);
        }
    }

}
