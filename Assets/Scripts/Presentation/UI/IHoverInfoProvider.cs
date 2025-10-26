using UnityEngine;

namespace ScreamHotel.UI
{
    public enum HoverKind { None, Room, Roof, ShopSlot, ShopReroll, TrainingRoom, TrainingRemain, Character }

    public struct HoverInfo
    {
        public HoverKind Kind;
        public string RoomId;         // Room
        public int NextFloor;         // Roof
        public int Cost;              // Roof
        public int ShopPrice;
        public string ShopGhostId;
    }

    public interface IHoverInfoProvider
    {
        HoverInfo GetHoverInfo();
    }

    public interface IClickActionProvider
    {
        bool TryClick(Core.Game game);
    }
}