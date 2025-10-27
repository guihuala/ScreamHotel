using UnityEngine;
using UnityEngine.UI;
using ScreamHotel.Core;
using ScreamHotel.Presentation;
using ScreamHotel.Systems;

namespace ScreamHotel.UI
{
    public class RoofHoverPanel : MonoBehaviour
    {
        [Header("UI")]
        public Canvas canvas;
        public RectTransform root;
        public Text titleText;
        public Text costText;
        public Button buildBtn;
        public Image bulidImage;

        [Header("Panel Position")]
        public Vector3 panelOffset = new Vector3(0f, 2f, 0f); // 面板相对于楼顶的世界偏移

        private Game _game;
        private Camera _mainCam;
        private Transform _targetTransform;    // 目标（楼顶/屋顶）Transform

        void Awake()
        {
            _game = FindObjectOfType<Game>();
            _mainCam = Camera.main;
            Hide();
        }

        public void Show(int nextFloor, int cost, Vector3? worldOffset = null)
        {
            if (!canvas) canvas = GetComponentInParent<Canvas>();
            _targetTransform = FindObjectOfType<RoofBuildZone>().transform;
            if (worldOffset.HasValue) panelOffset = worldOffset.Value;

            root.gameObject.SetActive(true);
            titleText.text = $"Build Floor {nextFloor}";
            costText.text = $"Cost: {cost}";
            
            bool isDayTime = _game?.TimeSystem?.GetCurrentTimePeriod() == GameState.Day;

            buildBtn.interactable = isDayTime;

            if (!isDayTime)
            {
                bulidImage.gameObject.SetActive(false);
                buildBtn.GetComponentInChildren<Text>().text = "Can't Build at Night";
            }
            else
            {
                bulidImage.gameObject.SetActive(true);
                buildBtn.GetComponentInChildren<Text>().text = "Build";
            }

            buildBtn.onClick.RemoveAllListeners();
            buildBtn.onClick.AddListener(() =>
            {
                if (isDayTime)
                {
                    var build = GetBuild();
                    if (build.TryBuildNextFloor(out var builtFloor))
                    {
                        AudioManager.Instance.PlaySfx("Build");
                    }
                }
            });

            PlacePanelAtTarget();
        }
        
        void Update()
        {
            if (root != null && root.gameObject.activeInHierarchy && _targetTransform != null)
            {
                PlacePanelAtTarget();
            }
        }

        private void PlacePanelAtTarget()
        {
            if (_targetTransform == null || canvas == null) return;

            Vector3 worldPos = _targetTransform.position + panelOffset;
            Vector3 screenPos = _mainCam != null ? _mainCam.WorldToScreenPoint(worldPos) : worldPos;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPos, canvas.worldCamera, out var localPos);

            root.anchoredPosition = localPos;
        }

        public void Hide()
        {
            if (root) root.gameObject.SetActive(false);
            _targetTransform = null;
        }

        private BuildSystem GetBuild()
        {
            var f = typeof(Game).GetField("_buildSystem",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (BuildSystem)f.GetValue(_game);
        }
    }
}
