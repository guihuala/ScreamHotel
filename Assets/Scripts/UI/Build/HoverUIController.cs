using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using ScreamHotel.Core;
using ScreamHotel.Presentation.Shop;

namespace ScreamHotel.UI
{
    public class HoverUIController : MonoBehaviour
    {
        [Header("Refs")]
        public Camera mainCamera;
        public RoomHoverPanel roomPanel;    // 你的屏幕空间UI
        public RoofHoverPanel roofPanel;    // 你的屏幕空间UI
        public ShopHoverPanel shopPanel;    // 新增：商店悬浮面板（世界/屏幕空间皆可）

        [Header("Raycast")]
        public LayerMask interactMask = ~0;
        public float rayMaxDistance = 500f;

        [Header("Behavior")]
        public bool blockWhenPointerOverUI = true;

        private Game _game;
        private HoverKind _lastKind = HoverKind.None;
        private string _lastRoomId;
        private int _lastShopIndex = -1;

        void Awake()
        {
            if (!mainCamera) mainCamera = Camera.main;
            _game = FindObjectOfType<Game>();
        }

        void Update()
        {
            // 1) 物理射线
            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, rayMaxDistance, interactMask))
            {
                HideAll();
                return;
            }

            // 2) 读取 Hover 信息
            var provider = hit.collider.GetComponentInParent<IHoverInfoProvider>();
            if (provider == null) { HideAll(); return; }
            var info = provider.GetHoverInfo();

            // 3) 展示对应面板
            ShowPanels(info);

            // 4) 点击行为（左键）
            if (Input.GetMouseButtonDown(0))
            {
                var clickable = hit.collider.GetComponentInParent<IClickActionProvider>();
                if (clickable != null && clickable.TryClick(_game))
                {
                    // 购买/建造成功：让表现层自己刷新
                    SendMessage("SyncShop", SendMessageOptions.DontRequireReceiver);
                    SendMessage("SyncAll",  SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        private void ShowPanels(HoverInfo info)
        {
            switch (info.Kind)
            {
                case HoverKind.Roof:
                    roofPanel?.Show(info.NextFloor, info.Cost, Input.mousePosition);
                    roomPanel?.Hide();
                    shopPanel?.Hide();
                    _lastKind = HoverKind.Roof;
                    _lastRoomId = null;
                    _lastShopIndex = -1;
                    break;

                case HoverKind.Room:
                    if (info.RoomId != _lastRoomId || _lastKind != HoverKind.Room)
                        roomPanel?.Show(info.RoomId);
                    roofPanel?.Hide();
                    shopPanel?.Hide();
                    _lastKind = HoverKind.Room;
                    _lastRoomId = info.RoomId;
                    _lastShopIndex = -1;
                    break;

                case HoverKind.ShopSlot:
                    var screen = (Vector3)( (Vector2)Input.mousePosition + info.ScreenOffset );
                    shopPanel?.Show(info.ShopMain, info.ShopPrice, screen);
                    roomPanel?.Hide();
                    roofPanel?.Hide();
                    _lastKind = HoverKind.ShopSlot;
                    _lastRoomId = null;
                    _lastShopIndex = info.ShopSlotIndex;
                    break;

                default:
                    HideAll();
                    break;
            }
        }

        private void HideAll()
        {
            roomPanel?.Hide();
            roofPanel?.Hide();
            shopPanel?.Hide();
            _lastKind = HoverKind.None;
            _lastRoomId = null;
            _lastShopIndex = -1;
        }
    }
}
