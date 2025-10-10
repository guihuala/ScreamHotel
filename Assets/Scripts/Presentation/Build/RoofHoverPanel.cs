using UnityEngine;
using UnityEngine.UI;
using ScreamHotel.Core;
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

        private Game _game;

        void Awake()
        {
            _game = FindObjectOfType<Game>();
            Hide();
        }

        public void Show(int nextFloor, int cost, Vector3 screenPos)
        {
            if (!canvas) canvas = GetComponentInParent<Canvas>();
            root.gameObject.SetActive(true);
            titleText.text = $"Build Floor {nextFloor}";
            costText.text = $"Cost: {cost}";

            // 按钮
            buildBtn.onClick.RemoveAllListeners();
            buildBtn.onClick.AddListener(() =>
            {
                var build = GetBuild();
                if (build.TryBuildNextFloor(out var builtFloor))
                {
                    // 成功后面板可以继续显示（鼠标仍在楼顶），数值会随 UpdateZone 更新
                }
            });
        }

        public void Hide()
        {
            if (root) root.gameObject.SetActive(false);
        }
        
        private BuildSystem GetBuild()
        {
            var f = typeof(Game).GetField("_buildSystem",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (BuildSystem)f.GetValue(_game);
        }
    }
}
