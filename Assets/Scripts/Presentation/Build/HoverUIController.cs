using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using ScreamHotel.Core;
using ScreamHotel.Presentation.Shop;
using UnityEngine.UI;
using ScreamHotel.Presentation;
using TMPro;

namespace ScreamHotel.UI
{
    public class HoverUIController : MonoBehaviour
    {
        [Header("Refs")]
        public Camera mainCamera;
        public RoomHoverPanel roomPanel;
        public RoofHoverPanel roofPanel;
        public ShopHoverPanel shopPanel;
        public ShopRerollHoverPanel shopRerollPanel;
        public PickFearPanel pickFearPanel;
        public TrainingRemainPanel trainingRemainPanel;

        [Header("Raycast")]
        public LayerMask interactMask = ~0;
        public float rayMaxDistance = 500f;
        
        [Header("Hover UI")]
        [Tooltip("鼠标悬停时面板的屏幕像素偏移（相对于鼠标位置）。")]
        public Vector2 hoverScreenOffset = new Vector2(100f, 16f);

        private Game _game;
        private HoverKind _lastKind = HoverKind.None;
        private string _lastRoomId;
        private bool _isPickFearPanelActive = false;
        private TrainingSlot _currentTrainingSlot; // 当前悬停的训练槽位

        void Awake()
        {
            if (!mainCamera) mainCamera = Camera.main;
            _game = FindObjectOfType<Game>();
        }

        void Update()
        {
            // 如果恐惧标签面板激活，暂停其他悬停逻辑
            if (_isPickFearPanelActive)
            {
                return;
            }

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

            // 3) 检查是否是训练槽位
            var trainingSlot = hit.collider.GetComponentInParent<TrainingSlot>();
            if (trainingSlot != null)
            {
                ShowTrainingSlotInfo(trainingSlot);
                _lastKind = HoverKind.TrainingRoom;
                return;
            }

            // 4) 展示对应面板
            ShowPanels(info);

            // 5) 点击行为（左键）
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

        private void ShowTrainingSlotInfo(TrainingSlot trainingSlot)
        {
            // 隐藏其他面板
            roomPanel?.Hide();
            roofPanel?.Hide();
            shopPanel?.Hide();
            shopRerollPanel?.Hide();

            // 显示训练剩余天数面板
            if (trainingRemainPanel != null && trainingSlot.IsTraining)
            {
                var game = FindObjectOfType<Game>();
                if (game != null)
                {
                    int trainingTime = game.World.Config.Rules.ghostTrainingTimeDays;
                    var ghost = game.World.Ghosts.FirstOrDefault(x => x.Id == trainingSlot.GhostId);
                    if (ghost != null)
                    {
                        int remainDays = Mathf.Max(0, trainingTime - ghost.TrainingDays);
                        trainingRemainPanel.Show(remainDays, trainingSlot.transform);
                        _currentTrainingSlot = trainingSlot;
                    }
                }
            }
            else
            {
                trainingRemainPanel?.Hide();
                _currentTrainingSlot = null;
            }

            _lastKind = HoverKind.TrainingRoom;
            _lastRoomId = null;
        }

        private void ShowPanels(HoverInfo info)
        {
            var screen = (Vector3)( (Vector2)Input.mousePosition + hoverScreenOffset );
            
            // 隐藏训练剩余天数面板
            trainingRemainPanel?.Hide();
            _currentTrainingSlot = null;

            switch (info.Kind)
            {
                case HoverKind.Roof:
                    roofPanel?.Show(info.NextFloor, info.Cost);
                    _lastKind = HoverKind.Roof;
                    _lastRoomId = null;
                    break;

                case HoverKind.Room:
                    if (info.RoomId != _lastRoomId || _lastKind != HoverKind.Room)
                        roomPanel?.Show(info.RoomId);
                    _lastKind = HoverKind.Room;
                    _lastRoomId = info.RoomId;
                    break;

                case HoverKind.ShopSlot:
                    shopPanel?.Show(info.ShopMain, info.ShopPrice, screen);
                    _lastKind = HoverKind.ShopSlot;
                    _lastRoomId = null;
                    break;
                
                case HoverKind.ShopReroll:
                {
                    shopRerollPanel?.Show(info.Cost, screen);
                    _lastKind = HoverKind.ShopReroll;
                    break;
                }

                case HoverKind.TrainingRoom:
                    // 训练室整体悬停，不显示具体信息
                    _lastKind = HoverKind.TrainingRoom;
                    _lastRoomId = null;
                    break;
                
                case HoverKind.TrainingRemain:
                    _lastKind = HoverKind.TrainingRoom;
                    _lastRoomId = null;
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
            shopRerollPanel?.Hide();
            trainingRemainPanel?.Hide();
            _lastKind = HoverKind.None;
            _lastRoomId = null;
            _currentTrainingSlot = null;
        }

        // === 恐惧标签选择面板相关方法 ===

        /// <summary>
        /// 打开恐惧标签选择面板（从TrainingRoomZone调用）
        /// </summary>
        public void OpenPickFearPanel(string ghostId, Transform transform,int slotIndex, System.Action<string, Domain.FearTag, int> onFearSelected)
        {
            if (pickFearPanel == null)
            {
                // 如果没有预制体引用，动态创建
                CreatePickFearPanel();
            }

            if (pickFearPanel != null)
            {
                _isPickFearPanelActive = true;
                pickFearPanel.Init(ghostId, slotIndex, transform,(selectedGhostId, tag, selectedSlotIndex) =>
                {
                    // 面板关闭时恢复悬停逻辑
                    _isPickFearPanelActive = false;
                    onFearSelected?.Invoke(selectedGhostId, tag, selectedSlotIndex);
                });
            }
        }

        /// <summary>
        /// 动态创建恐惧标签选择面板
        /// </summary>
        private void CreatePickFearPanel()
        {
            var panelObj = new GameObject("PickFearPanel");
            panelObj.transform.SetParent(transform, false);
            pickFearPanel = panelObj.AddComponent<PickFearPanel>();
            
            // 设置Canvas
            var canvas = panelObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 确保在最上层
            
            panelObj.AddComponent<GraphicRaycaster>();
        }
        
        /// <summary>
        /// 检查是否有激活的恐惧标签面板
        /// </summary>
        public bool IsPickFearPanelActive()
        {
            return _isPickFearPanelActive;
        }
    }
}