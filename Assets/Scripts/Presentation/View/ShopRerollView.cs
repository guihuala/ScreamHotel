using UnityEngine;
using ScreamHotel.UI;
using ScreamHotel.Core;

namespace ScreamHotel.Presentation.Shop
{
    [RequireComponent(typeof(Collider))]
    public class ShopRerollView : MonoBehaviour, IHoverInfoProvider, IClickActionProvider
    {
        public HoverInfo GetHoverInfo()
        {
            var game = FindObjectOfType<Game>();
            int cost = game?.World?.Config?.Rules?.ghostShopRerollCost ?? 0;
            return new HoverInfo
            {
                Kind = HoverKind.ShopReroll,
                Cost = cost,
            };
        }
        
        public bool TryClick(Game game)
        {
            if (game == null) return false;
            bool ok = game.ShopTryReroll();
            if (ok)
            {
                // 刷新商店可视
                var pc = FindObjectOfType<PresentationController>();
                if (pc) pc.SendMessage("SyncShop", SendMessageOptions.DontRequireReceiver);
            }
            return ok;
        }
    }
}