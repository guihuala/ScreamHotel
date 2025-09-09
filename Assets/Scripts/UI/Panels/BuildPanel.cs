using UnityEngine;
using UnityEngine.UI;
using ScreamHotel.Core;
using ScreamHotel.Systems;
using ScreamHotel.Domain;
using System;
using System.Linq;

namespace ScreamHotel.UI
{
    public class BuildPanel : MonoBehaviour
    {
        [Header("References")]
        private Game game;
        public Button buyRoomButton;
        public Button upgradeRoomButton;
        public InputField roomIdInput;
        public Dropdown tagDropdown;
        public Text goldText;
        public Text feedbackText;

        private BuildSystem _build => GetBuild();

        private void Awake()
        {
            game = FindObjectOfType<Game>();
            
            if (buyRoomButton) buyRoomButton.onClick.AddListener(OnClickBuyRoom);
            if (upgradeRoomButton) upgradeRoomButton.onClick.AddListener(OnClickUpgradeRoom);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GoldChanged>(OnGoldChanged);
            EventBus.Subscribe<RoomPurchasedEvent>(OnRoomPurchased);
            EventBus.Subscribe<RoomUpgradedEvent>(OnRoomUpgraded);
            RefreshGoldUI();
            InitTagDropdown();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GoldChanged>(OnGoldChanged);
            EventBus.Unsubscribe<RoomPurchasedEvent>(OnRoomPurchased);
            EventBus.Unsubscribe<RoomUpgradedEvent>(OnRoomUpgraded);
        }

        // 通过反射拿到 Game 中创建的 BuildSystem 实例（避免你改 Game 的可见性）
        private BuildSystem GetBuild()
        {
            var field = typeof(Game).GetField("_buildSystem",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (BuildSystem)field.GetValue(game);
        }

        public void OnClickBuyRoom()
        {
            if (_build.TryBuyRoom(out var id))
                SetFeedback($"Bought new room: {id}");
            else
                SetFeedback("Not enough gold to buy room.");

            RefreshGoldUI();
        }

        public void OnClickUpgradeRoom()
        {
            var id = !string.IsNullOrEmpty(roomIdInput?.text) ? roomIdInput.text : AutoPickFirstUpgradeable();
            if (string.IsNullOrEmpty(id)) { SetFeedback("No room selected / no upgradeable room."); return; }

            FearTag? tag = null;
            if (tagDropdown != null && tagDropdown.value >= 0)
                tag = (FearTag)tagDropdown.value;

            if (_build.TryUpgradeRoom(id, tag))
                SetFeedback($"Upgraded {id} to next level.");
            else
                SetFeedback("Upgrade failed: not enough gold or room already max.");

            RefreshGoldUI();
        }

        private string AutoPickFirstUpgradeable()
        {
            var r = game.World.Rooms.FirstOrDefault(x => x.Level < 3);
            return r?.Id;
        }

        private void OnGoldChanged(GoldChanged g) => RefreshGoldUI();
        private void OnRoomPurchased(RoomPurchasedEvent e) { /* 可刷新房间列表 */ }
        private void OnRoomUpgraded(RoomUpgradedEvent e) { /* 可刷新指定房间显示 */ }

        private void RefreshGoldUI()
        {
            if (goldText != null) goldText.text = $"Gold: {game.World.Economy.Gold}";
        }

        private void SetFeedback(string s)
        {
            if (feedbackText != null) feedbackText.text = s;
            else Debug.Log(s);
        }

        private void InitTagDropdown()
        {
            if (tagDropdown == null) return;
            tagDropdown.ClearOptions();
            var names = Enum.GetNames(typeof(FearTag));
            tagDropdown.AddOptions(names.ToList());
        }
    }
}
