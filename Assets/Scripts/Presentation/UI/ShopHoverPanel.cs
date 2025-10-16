using UnityEngine;
using UnityEngine.UI;

namespace ScreamHotel.Presentation.Shop
{
    public class ShopHoverPanel : MonoBehaviour
    {
        public Text titleText;
        public Text priceText;

        public void SetContent(Domain.FearTag main, int price)
        {
            if (titleText) titleText.text = $"{main}";
            if (priceText) priceText.text = $"Price:{price}";
        }
        
        public void Show(Domain.FearTag main, int price, Vector3 screenPos)
        {
            SetContent(main, price);
            transform.position = screenPos;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}