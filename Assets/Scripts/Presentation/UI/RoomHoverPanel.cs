using System;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using ScreamHotel.Core;
using ScreamHotel.Domain;

namespace ScreamHotel.UI
{
    /// <summary>
    /// 房间悬浮面板：仅展示状态与提示
    /// 建造/升级入口改为：白天把【鬼】拖到房间，由 RoomDropZone 处理
    /// </summary>
    public class RoomHoverPanel : MonoBehaviour
    {
        [Header("UI")]
        public Canvas canvas;
        public RectTransform root;
        public Text titleText;
        public Text infoText;

        [Header("Panel Position")]
        public Vector3 panelOffset = new Vector3(0f, 2f, 0f);

        private Game _game;
        private Camera mainCamera;
        private Transform _currentRoomTransform;

        void Awake()
        {
            _game = FindObjectOfType<Game>();
            mainCamera = Camera.main;
        }

        private void Start() => Hide();

        public void Show(string roomId)
        {
            var game = FindObjectOfType<Game>();
            if (game == null || game.World == null) { Hide(); return; }

            var room = game.World.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (room == null) { Hide(); return; }

            var roomView = FindRoomView(roomId);
            if (roomView == null) { Hide(); return; }

            if (!canvas) canvas = GetComponentInParent<Canvas>();
            _currentRoomTransform = roomView.transform;

            if (root) root.gameObject.SetActive(true);
            if (titleText) titleText.text = room.Id;

            string tagText = room.RoomTag.HasValue ? room.RoomTag.Value.ToString() : "-";
            string line1 = $"Lv {room.Level} | Cap {room.Capacity} | Tag {tagText}";

            string hint = "提示：在【白天】将鬼拖入房间可进行建造/升级（持续 1~2 秒并播放特效）。Lv1→Lv2 会先选择恐惧属性，只有选择后才会继续。";
            if (infoText) infoText.text = $"{line1}\n{hint}";
            
            PlacePanelAtRoom();
        }

        void Update()
        {
            if (root != null && root.gameObject.activeInHierarchy && _currentRoomTransform != null)
            {
                PlacePanelAtRoom();
            }
        }

        private void PlacePanelAtRoom()
        {
            if (_currentRoomTransform == null || canvas == null) return;

            Vector3 worldPosition = _currentRoomTransform.position + panelOffset;
            Vector3 screenPosition = mainCamera != null
                ? mainCamera.WorldToScreenPoint(worldPosition)
                : worldPosition;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPosition, canvas.worldCamera, out var localPos);

            root.anchoredPosition = localPos;
        }

        private ScreamHotel.Presentation.RoomView FindRoomView(string roomId)
        {
            var roomViews = FindObjectsOfType<ScreamHotel.Presentation.RoomView>();
            return roomViews.FirstOrDefault(rv => rv.roomId == roomId);
        }

        public void Hide()
        {
            if (root) root.gameObject.SetActive(false);
            _currentRoomTransform = null;
        }
    }
}
