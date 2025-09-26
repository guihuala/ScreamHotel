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
        private BoxCollider _box;
        private Game _game;

        void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
            _game = FindObjectOfType<Game>();
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
            return (BuildSystem)f.GetValue(_game);
        }
    }
}