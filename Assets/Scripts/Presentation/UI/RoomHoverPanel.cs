using System;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using ScreamHotel.Core;
using ScreamHotel.Domain;

namespace ScreamHotel.UI
{
    public class RoomHoverPanel : MonoBehaviour
    {
        [Header("UI")]
        public Canvas canvas;
        public RectTransform root;
        public Text titleText;

        [Header("Fear Icon")]
        [Tooltip("用于显示当前房间恐惧属性的图标（可放在面板的一角）")]
        public Image fearIconImage;
        [Tooltip("Tag→Sprite 的映射表，做法与 FearIconsPanel 一致")]
        public FearIconAtlas fearIconAtlas;

        [Header("Panel Position")]
        public Vector3 panelOffset = new Vector3(0f, 2f, 0f);

        private Game _game;
        private Camera mainCamera;
        private Transform _currentRoomTransform;

        void Awake()
        {
            _game = FindObjectOfType<Game>();
            mainCamera = Camera.main;

            // 初始隐藏图标位
            if (fearIconImage) fearIconImage.gameObject.SetActive(false);
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
            
            if (titleText)
            {
                int number = ParseRoomNumber(room.Id);       // e.g. room_F2_L1 -> 201
                titleText.text = $"{number:000}  Lv.{room.Level}";
            }
            
            if (fearIconImage)
            {
                if (room.RoomTag.HasValue && fearIconAtlas != null)
                {
                    var sprite = fearIconAtlas.Get(room.RoomTag.Value);
                    if (sprite != null)
                    {
                        fearIconImage.sprite = sprite;
                        fearIconImage.preserveAspect = true;   // 防止拉伸（与 FearIconsPanel 一致）
                        fearIconImage.raycastTarget = false;   // 不遮挡交互
                        fearIconImage.enabled = true;
                        fearIconImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        // 找不到图就隐藏
                        fearIconImage.sprite = null;
                        fearIconImage.enabled = false;
                        fearIconImage.gameObject.SetActive(false);
                        Debug.LogWarning($"[RoomHoverPanel] Sprite not found for tag {room.RoomTag.Value} in atlas.");
                    }
                }
                else
                {
                    fearIconImage.sprite = null;
                    fearIconImage.enabled = false;
                    fearIconImage.gameObject.SetActive(false);
                }
            }

            PlacePanelAtRoom();
        }
        
        private int ParseRoomNumber(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;

            int floor = 0;
            string slot = null;

            // 提取楼层
            int fIdx = id.IndexOf("_F", StringComparison.Ordinal);
            if (fIdx >= 0)
            {
                int usIdx = id.IndexOf('_', fIdx + 2);
                if (usIdx > fIdx + 2)
                {
                    var num = id.Substring(fIdx + 2, usIdx - (fIdx + 2));
                    int.TryParse(num, out floor);
                    slot = id[(usIdx + 1)..]; // 剩余部分视作槽位
                }
            }

            // 槽位→序号：L1=01, L2=02, R1=03, R2=04
            // 兼容旧命名：LA->01, LB->02, RA->03, RB->04
            int idx = slot switch
            {
                "L1" => 1, "LA" => 1,
                "L2" => 2, "LB" => 2,
                "R1" => 3, "RA" => 3,
                "R2" => 4, "RB" => 4,
                _    => 0
            };

            // 生成： floor*100 + idx，例如 F2 & R2 => 2*100 + 4 = 204
            return Mathf.Clamp(floor, 0, 99) * 100 + Mathf.Clamp(idx, 0, 99);
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

        private Presentation.RoomView FindRoomView(string roomId)
        {
            var roomViews = FindObjectsOfType<ScreamHotel.Presentation.RoomView>();
            return roomViews.FirstOrDefault(rv => rv.roomId == roomId);
        }

        public void Hide()
        {
            if (root) root.gameObject.SetActive(false);
            _currentRoomTransform = null;

            if (fearIconImage)
            {
                fearIconImage.sprite = null;
                fearIconImage.enabled = false;
                fearIconImage.gameObject.SetActive(false);
            }
        }
    }
}
