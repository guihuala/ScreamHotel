using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using ScreamHotel.Core;
using ScreamHotel.Systems;
using ScreamHotel.Domain;

namespace ScreamHotel.UI
{
    public class RoomHoverPanel : MonoBehaviour
    {
        [Header("UI")]
        public Canvas canvas;
        public RectTransform root;
        public Text titleText;     // Room_F1_LA
        public Text infoText;      // Lv/Cap/Tag
        public Button upgradeBtn;  // 升级按钮
        public Dropdown tagDropdownForLv2; // 可选：Lv1->Lv2 时选择Tag

        [Header("Panel Position")]
        public Vector3 panelOffset = new Vector3(0, 2f, 0); // 面板相对于房间的偏移

        private Game _game;
        private Camera mainCamera;
        private Transform _currentRoomTransform; // 当前显示面板的房间

        void Awake()
        {
            _game = FindObjectOfType<Game>();
            mainCamera = Camera.main;
            
            Hide();

            if (tagDropdownForLv2)
            {
                tagDropdownForLv2.ClearOptions();
                tagDropdownForLv2.AddOptions(System.Enum.GetNames(typeof(FearTag)).ToList());
            }
        }

        public void Show(string roomId, Vector3 worldPosition)
        {
            if (!canvas) canvas = GetComponentInParent<Canvas>();
            var w = _game.World;
            var r = w.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null) { Hide(); return; }

            // 查找房间的Transform（通过RoomView）
            var roomView = FindRoomView(roomId);
            if (roomView == null) { Hide(); return; }

            _currentRoomTransform = roomView.transform;
            
            root.gameObject.SetActive(true);
            titleText.text = roomId;
            var tag = r.RoomTag.HasValue ? r.RoomTag.Value.ToString() : "-";
            infoText.text = $"Lv {r.Level} | Cap {r.Capacity} | Tag {tag}";

            // 设置面板位置（固定在房间上方）
            PlacePanelAtRoom();

            // 升级按钮
            upgradeBtn.interactable = r.Level < 3;
            upgradeBtn.onClick.RemoveAllListeners();
            upgradeBtn.onClick.AddListener(() =>
            {
                FearTag? sel = null;
                if (r.Level == 1 && tagDropdownForLv2) sel = (FearTag)tagDropdownForLv2.value;

                var build = GetBuild();
                if (build.TryUpgradeRoom(roomId, sel))
                {
                    // 升级成功后刷新显示
                    var rr = w.Rooms.First(x => x.Id == roomId);
                    var t2 = rr.RoomTag.HasValue ? rr.RoomTag.Value.ToString() : "-";
                    infoText.text = $"Lv {rr.Level} | Cap {rr.Capacity} | Tag {t2}";
                    
                    // 刷新按钮状态
                    upgradeBtn.interactable = rr.Level < 3;
                }
            });
        }

        void Update()
        {
            // 如果面板显示中，持续更新位置以跟随房间移动（如果需要）
            if (root.gameObject.activeInHierarchy && _currentRoomTransform != null)
            {
                PlacePanelAtRoom();
            }
        }

        private void PlacePanelAtRoom()
        {
            if (_currentRoomTransform == null) return;

            // 计算面板的世界位置（房间位置 + 偏移）
            Vector3 worldPosition = _currentRoomTransform.position + panelOffset;
            
            // 转换为屏幕坐标
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            
            // 转换为UI的局部坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPosition, canvas.worldCamera, out var localPos);
            
            root.anchoredPosition = localPos;
        }

        private Presentation.RoomView FindRoomView(string roomId)
        {
            // 查找场景中对应的RoomView
            var roomViews = FindObjectsOfType<ScreamHotel.Presentation.RoomView>();
            return roomViews.FirstOrDefault(rv => rv.roomId == roomId);
        }

        public void Hide()
        {
            if (root) root.gameObject.SetActive(false);
            _currentRoomTransform = null;
        }

        private BuildSystem GetBuild()
        {
            var f = typeof(Game).GetField("_buildSystem",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (BuildSystem)f.GetValue(_game);
        }
    }
}