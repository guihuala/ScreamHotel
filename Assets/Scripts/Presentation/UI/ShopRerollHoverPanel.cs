using UnityEngine;
using UnityEngine.UI;

namespace ScreamHotel.Presentation.Shop
{
    public class ShopRerollHoverPanel : MonoBehaviour
    {
        public Text titleText;  // “刷新货架”
        public Text costText;   // “价格：-X”

        public void Show(int cost, Vector3 screenPos)
        {
            if (titleText) titleText.text = "Retroll";
            if (costText)  costText.text  = $"Price:{cost}";
            transform.position = screenPos;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}