using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Systems;

namespace ScreamHotel.UI
{
    /// <summary>
    /// 放在一个空物体上，自动把自身 BoxCollider 放到“最高层上方”，用于鼠标悬停显示"建造下一层"。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class RoofBuildZone : MonoBehaviour
    {
        [Header("Layout (relative to roomsRoot)")]
        public Transform roomsRoot;      // 与 PresentationController 的 roomsRoot 一致
        public float roomBaseY = 0f;
        public float floorSpacing = 8f;
        public float roofOffsetY = 2f;   // 楼顶比最高层中心再高一点
        public Vector2 roofSize = new Vector2(20f, 6f); // X 宽度 × Z 深度

        private BoxCollider _box;
        private Game _game;

        void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
            _game = FindObjectOfType<Game>();
            UpdateZone();
        }

        void Update()
        {
            UpdateZone(); // 简单起见每帧更新；你也可监听 FloorBuiltEvent 再更新
        }

        public int GetNextFloor()
        {
            var build = GetBuild();
            return build.GetNextFloorIndex();
        }

        public int GetNextFloorCost()
        {
            var build = GetBuild();
            return build.GetFloorBuildCost(build.GetNextFloorIndex());
        }

        private BuildSystem GetBuild()
        {
            var f = typeof(Game).GetField("_buildSystem",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (BuildSystem)f.GetValue(_game); // 复用你UI里拿 BuildSystem 的方式 :contentReference[oaicite:3]{index=3}
        }

        private void UpdateZone()
        {
            var build = GetBuild();
            int highest = build.GetHighestFloor(); // 0 表示还没有房间/楼层 :contentReference[oaicite:4]{index=4}

            float topY = roomBaseY + Mathf.Max(0, highest - 1) * floorSpacing + roofOffsetY;
            Vector3 localCenter = new Vector3(0f, topY, 0f);
            Vector3 worldCenter = roomsRoot ? roomsRoot.TransformPoint(localCenter) : localCenter;

            _box.center = transform.InverseTransformPoint(worldCenter);
            _box.size   = new Vector3(roofSize.x, 0.1f, roofSize.y);
        }
    }
}
