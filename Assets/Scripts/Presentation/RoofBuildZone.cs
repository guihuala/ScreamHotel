using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Systems;
using ScreamHotel.UI;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(BoxCollider))]
    public class RoofBuildZone : MonoBehaviour, IHoverInfoProvider
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
        
        public HoverInfo GetHoverInfo() => new HoverInfo
        {
            Kind = HoverKind.Roof,
            NextFloor = GetNextFloor(),
            Cost = GetNextFloorCost(),
            WorldPosition = transform.position
        };
    }
}