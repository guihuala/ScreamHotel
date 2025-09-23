using ScreamHotel.Core;
using ScreamHotel.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScreamHotel.UI
{
    public class HudController : MonoBehaviour
    {
        [Header("References")] private Game game;
        public Button pauseButton;
        public TextMeshProUGUI goldText;

        private void Awake()
        {
            game = FindObjectOfType<Game>();

            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }
        
        private void OnEnable()
        {
            EventBus.Subscribe<GoldChanged>(OnGoldChanged);
            RefreshGoldUI();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GoldChanged>(OnGoldChanged);
        }

        private void OnGoldChanged(GoldChanged g) => RefreshGoldUI();

        private void OnPauseButtonClicked()
        {
            UIManager.Instance.OpenPanel("PausePanel");
        }

        private void RefreshGoldUI()
        {
            if (goldText != null) goldText.text = $"Gold: {game.World.Economy.Gold}";
        }
    }
}