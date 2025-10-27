using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.Systems;
using ScreamHotel.Presentation;

namespace ScreamHotel.UI
{
    public class BuildRoomPanel : MonoBehaviour
    {
        [Header("Atlas & Button")]
        [Tooltip("Tag→Sprite 的映射表；和 FearIconsPanel 用法一致")]
        public FearIconAtlas fearIconAtlas;

        [Header("UI Refs")]
        public Canvas canvas;
        public RectTransform root;
        public Text costText;               // 费用文案
        public Button buildBtn;             // Unlock / Upgrade 按钮
        public Text buildBtnLabel;          // 可选：按钮文字(Unlock/Upgrade)

        [Header("Lv1 -> Lv2 Fear Selection")]
        public GameObject fearTagSelectionPanel;
        public Transform buttonContainer;   // 存放恐惧属性按钮的容器

        [Header("Positioning")]
        [Tooltip("面板在房间上方的世界偏移（与 RoomHoverPanel 一致）")]
        public Vector3 panelOffset = new Vector3(0f, 2f, 0f);

        // --- Private state ---
        private Game _game;
        private Room _room;

        private int  buildCost;     // 升级费用（Lv1->Lv2 / Lv2->Lv3）
        private int  unlockCost;    // 解锁费用（Lv0->Lv1）
        private bool isLv1ToLv2Upgrade;

        private Camera   _mainCamera;
        private Transform _currentRoomTransform;

        // Lv1->Lv2 选择的恐惧属性 & 高亮状态
        private FearTag? _selectedTag;
        private Image    _lastSelectedImage;

        void Awake()
        {
            _game = FindObjectOfType<Game>();
            if (!canvas) canvas = GetComponentInParent<Canvas>();
            if (!root) root = GetComponent<RectTransform>();
            _mainCamera = Camera.main;

            if (buildBtn) buildBtn.onClick.AddListener(OnBuildClicked);
        }

        private void Start()
        {
            Hide();
        }

        void Update()
        {
            // 面板激活时持续跟随房间位置
            if (root != null && root.gameObject.activeInHierarchy && _currentRoomTransform != null)
                PlacePanelAtRoom();
        }

        /// <summary>
        /// 显示并初始化面板：根据 roomId 锁定房间位置、展示费用与可选项
        /// </summary>
        public void Init(Room room)
        {
            _room = room;
            var rules = _game.World.Config.Rules;

            // 费用与按钮文案
            if (_room.Level == 0)
            {
                unlockCost = rules.roomUnlockCost;
                buildCost  = 0;

                if (buildBtnLabel) buildBtnLabel.text = "Unlock";
                if (costText)      costText.text      = $"Unlock Cost: {unlockCost} Gold";
            }
            else
            {
                buildCost  = _game.BuildSystem.GetRoomUpgradeCost(_room.Level);
                unlockCost = 0;

                if (buildBtnLabel) buildBtnLabel.text = "Upgrade";
                if (costText)      costText.text      = $"Upgrade Cost: {buildCost} Gold";
            }

            // 通过 roomId 查找对应的 RoomView，并固定位置（与 RoomHoverPanel 同思路）
            var rv = FindRoomView(_room.Id);     // 多房间场景下按 id 精准定位
            _currentRoomTransform = rv != null ? rv.transform : null;
            if (!canvas) canvas = GetComponentInParent<Canvas>();
            PlacePanelAtRoom();

            // Lv1 -> Lv2 需要先选择恐惧属性
            isLv1ToLv2Upgrade = _room.Level == 1;
            if (fearTagSelectionPanel) fearTagSelectionPanel.SetActive(isLv1ToLv2Upgrade);

            ClearFearTagButtons();
            _selectedTag = null;

            if (isLv1ToLv2Upgrade)
                CreateFearTagButtons();

            // 根据金币与是否选完属性，刷新按钮交互
            UpdateBuildButtonState();

            Show();
        }

        // ====== 生成/清理 Lv1->Lv2 选择按钮 ======

        private void CreateFearTagButtons()
        {
            if (buttonContainer == null) return;

            var fearTags = (FearTag[])Enum.GetValues(typeof(FearTag));
            foreach (var tag in fearTags)
            {
                var go = new GameObject(tag.ToString(), typeof(RectTransform));
                go.transform.SetParent(buttonContainer, false);

                var btn = go.AddComponent<Button>();
                var img = go.AddComponent<Image>();

                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(60, 60);

                // 图标
                Sprite icon = fearIconAtlas ? fearIconAtlas.Get(tag) : null;
                if (icon != null)
                {
                    img.sprite = icon;
                    img.preserveAspect = true;
                    img.color = Color.white;
                }
                else
                {
                    img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                }

                btn.onClick.AddListener(() => OnFearTagPicked(tag, img));
            }
        }

        private void ClearFearTagButtons()
        {
            if (buttonContainer == null) return;
            for (int i = buttonContainer.childCount - 1; i >= 0; i--)
            {
                var child = buttonContainer.GetChild(i);
                if (Application.isPlaying) Destroy(child.gameObject);
                else                       DestroyImmediate(child.gameObject);
            }
            _lastSelectedImage = null;
        }

        private void OnFearTagPicked(FearTag tag, Image img)
        {
            _selectedTag = tag;

            // 清除上一个高亮
            if (_lastSelectedImage != null)
            {
                var lastOutline = _lastSelectedImage.GetComponent<Outline>();
                if (lastOutline) Destroy(lastOutline);
            }

            // 新的高亮：给选中的图标加 Outline 作为“框选”
            var outline = img.GetComponent<Outline>() ?? img.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.yellow;
            outline.effectDistance = new Vector2(2f, 2f);

            _lastSelectedImage = img;

            UpdateBuildButtonState();
        }

        // ====== 按钮点击 ======
        private void OnBuildClicked()
        {
            // Lv0 -> Lv1：解锁
            if (_room.Level == 0)
            {
                DoBuildWithFx(() => _game.BuildSystem.TryUnlockRoom(_room.Id));
                return;
            }

            // Lv1 -> Lv2：需要先选恐惧属性
            if (isLv1ToLv2Upgrade)
            {
                if (!_selectedTag.HasValue) return;
                var chosen = _selectedTag.Value;
                DoBuildWithFx(() => _game.BuildSystem.TryUpgradeRoom(_room.Id, chosen));
                return;
            }

            // Lv2 -> Lv3
            DoBuildWithFx(() => _game.BuildSystem.TryUpgradeRoom(_room.Id));
        }

        private void UpdateBuildButtonState()
        {
            if (buildBtn == null) return;

            int need = (_room.Level == 0)
                ? _game.World.Config.Rules.roomUnlockCost
                : _game.BuildSystem.GetRoomUpgradeCost(_room.Level);

            bool haveGold = _game.World.Economy.Gold >= need;
            bool tagReady = !isLv1ToLv2Upgrade || _selectedTag.HasValue;

            buildBtn.interactable = haveGold && tagReady;
        }
        
        // === 工具方法 – 找到本房间的 RoomDropZone ===
        private RoomDropZone FindRoomDropZone()
        {
            var views = FindObjectsOfType<RoomView>();
            var rv = views.FirstOrDefault(v => v.roomId == _room.Id);
            return rv != null ? rv.GetComponent<RoomDropZone>() : null;
        }

        // === 新增：包装执行（带特效+音效）===
        private void DoBuildWithFx(Func<bool> buildAction)
        {
            var dz = FindRoomDropZone();
            if (dz != null)
            {
                dz.PlayBuildFxAndRun(buildAction);
            }
            else
            {
                // 找不到落点也允许直接执行，至少不阻塞流程
                buildAction?.Invoke();
            }

            // UI 先收起，特效在世界中播放即可
            Hide();
        }

        // ====== 面板定位（与 RoomHoverPanel 同步的实现） ======

        private void PlacePanelAtRoom()
        {
            if (_currentRoomTransform == null || canvas == null || root == null) return;

            Vector3 worldPos = _currentRoomTransform.position + panelOffset;
            Vector3 screenPos = _mainCamera != null
                ? _mainCamera.WorldToScreenPoint(worldPos)
                : worldPos;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPos, canvas.worldCamera, out var localPos);
            root.anchoredPosition = localPos;
        }

        private RoomView FindRoomView(string roomId)
        {
            var views = FindObjectsOfType<RoomView>();
            return views.FirstOrDefault(v => v.roomId == roomId);
        }

        // ====== 显隐 ======
        public void Show()
        {
            if (root) root.gameObject.SetActive(true);
            if (_currentRoomTransform != null) PlacePanelAtRoom();
        }

        public void Hide()
        {
            if (root) root.gameObject.SetActive(false);
            _currentRoomTransform = null;
            _selectedTag = null;

            // 清理高亮
            if (_lastSelectedImage != null)
            {
                var lastOutline = _lastSelectedImage.GetComponent<Outline>();
                if (lastOutline) Destroy(lastOutline);
                _lastSelectedImage = null;
            }
        }
    }
}
