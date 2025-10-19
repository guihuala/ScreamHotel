using System;
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
        public Text titleText;     // 房间名，如 Room_F1_LA
        public Text infoText;      // Lv/Cap/Tag
        public Button upgradeBtn;  // 升级按钮
        public Dropdown tagDropdownForLv2; // Lv1->Lv2 时选择Tag

        [Header("Panel Position")]
        [Tooltip("面板相对于房间在世界坐标的偏移")]
        public Vector3 panelOffset = new Vector3(0f, 2f, 0f); // 面板相对于房间的偏移

        private Game _game;
        private Camera mainCamera;
        private Transform _currentRoomTransform; // 当前显示面板的房间

        void Awake()
        {
            _game = FindObjectOfType<Game>();
            mainCamera = Camera.main;

            if (tagDropdownForLv2)
            {
                tagDropdownForLv2.ClearOptions();
                tagDropdownForLv2.AddOptions(Enum.GetNames(typeof(FearTag)).ToList());
            }
        }

        private void Start()
        {
            Hide();
        }

        /// <summary>
        /// 显示并固定在指定房间位置；房间名显示在 titleText
        /// </summary>
        public void Show(string roomId)
        {
            // 基础引用
            var game = FindObjectOfType<Game>();
            if (game == null || game.World == null)
            {
                Hide();
                return;
            }

            var world = game.World;
            var room = world.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (room == null)
            {
                Hide();
                return;
            }

            // 找到对应的 RoomView，用于定位
            var roomView = FindRoomView(roomId);
            if (roomView == null)
            {
                Hide();
                return;
            }

            if (!canvas) canvas = GetComponentInParent<Canvas>();
            _currentRoomTransform = roomView.transform;

            // 面板可见
            if (root) root.gameObject.SetActive(true);

            // 标题显示房间名称
            if (titleText) titleText.text = room.Id; // 如需更友好显示，可在此做映射

            // 显示房间当前信息
            string tagText = room.RoomTag.HasValue ? room.RoomTag.Value.ToString() : "-";
            if (infoText) infoText.text = $"Lv {room.Level} | Cap {room.Capacity} | Tag {tagText}";

            // 准备按钮
            if (upgradeBtn)
            {
                upgradeBtn.onClick.RemoveAllListeners();

                // 获取建造系统与规则
                var build = GetBuild();
                var rules = world.Config?.Rules;

                // UI：根据等级切换为“解锁/升级”
                var btnText = upgradeBtn.GetComponentInChildren<Text>();
                if (room.Level == 0)
                {
                    // —— Lv0：解锁 ——
                    if (btnText) btnText.text = "Unlock";
                    upgradeBtn.interactable = rules != null && world.Economy.Gold >= rules.roomUnlockCost;

                    // 解锁不需要选择 Tag
                    if (tagDropdownForLv2) tagDropdownForLv2.gameObject.SetActive(false);

                    upgradeBtn.onClick.AddListener(() =>
                    {
                        if (build != null && build.TryUnlockRoom(roomId))
                        {
                            // 刷新 UI
                            var rr = world.Rooms.First(x => x.Id == roomId);
                            string t2 = rr.RoomTag.HasValue ? rr.RoomTag.Value.ToString() : "-";
                            if (infoText) infoText.text = $"Lv {rr.Level} | Cap {rr.Capacity} | Tag {t2}";

                            // 解锁成功后切换为“升级”形态
                            if (btnText) btnText.text = "Upgrade";
                            upgradeBtn.interactable = rr.Level < 3;

                            if (tagDropdownForLv2)
                            {
                                tagDropdownForLv2.gameObject.SetActive(true);
                                tagDropdownForLv2.value = 0;
                                tagDropdownForLv2.RefreshShownValue();
                            }
                        }
                        else
                        {
                            upgradeBtn.interactable = false;
                        }
                    });
                }
                else
                {
                    // —— Lv1/Lv2：升级；Lv3：满级禁用 ——
                    if (btnText) btnText.text = "升级";
                    upgradeBtn.interactable = room.Level < 3;

                    // Lv1->Lv2 需要选择 Tag；Lv2->Lv3 不需要
                    if (tagDropdownForLv2) tagDropdownForLv2.gameObject.SetActive(room.Level == 1);

                    upgradeBtn.onClick.AddListener(() =>
                    {
                        // 计算（可选）Tag：仅在 Lv1->Lv2 时读取下拉
                        FearTag? sel = null;
                        if (room.Level == 1 && tagDropdownForLv2)
                        {
                            sel = (FearTag)tagDropdownForLv2.value;
                        }

                        if (build != null && build.TryUpgradeRoom(roomId, sel))
                        {
                            // 升级成功：刷新 UI
                            var rr = world.Rooms.First(x => x.Id == roomId);
                            string t2 = rr.RoomTag.HasValue ? rr.RoomTag.Value.ToString() : "-";
                            if (infoText) infoText.text = $"Lv {rr.Level} | Cap {rr.Capacity} | Tag {t2}";

                            upgradeBtn.interactable = rr.Level < 3;

                            // 升到 Lv2 后，下次点击是 Lv2->Lv3，不需要 Tag
                            if (tagDropdownForLv2) tagDropdownForLv2.gameObject.SetActive(rr.Level == 1);
                        }
                        else
                        {
                            // 失败（金币不足或已满级等），禁用按钮避免误操作
                            upgradeBtn.interactable = false;
                        }
                    });
                }
            }

            // 初次定位一次；后续在 Update 中持续跟随
            PlacePanelAtRoom();
        }

        void Update()
        {
            // 如果面板显示中，持续更新位置以跟随房间移动
            if (root != null && root.gameObject.activeInHierarchy && _currentRoomTransform != null)
            {
                PlacePanelAtRoom();
            }
        }

        private void PlacePanelAtRoom()
        {
            if (_currentRoomTransform == null || canvas == null) return;

            // 计算面板的世界位置（房间位置 + 偏移）
            Vector3 worldPosition = _currentRoomTransform.position + panelOffset;

            // 转换为屏幕坐标
            Vector3 screenPosition = mainCamera != null
                ? mainCamera.WorldToScreenPoint(worldPosition)
                : worldPosition;

            // 转换为UI的局部坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPosition, canvas.worldCamera, out var localPos);

            root.anchoredPosition = localPos;
        }

        private ScreamHotel.Presentation.RoomView FindRoomView(string roomId)
        {
            // 查找场景中对应的 RoomView（需确保每个 RoomView 的 roomId 唯一）
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