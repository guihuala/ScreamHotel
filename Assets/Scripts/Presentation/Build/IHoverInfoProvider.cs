using UnityEngine;

namespace ScreamHotel.UI
{
    public enum HoverKind { None, Room, Roof, ShopSlot, ShopReroll, TrainingRoom, TrainingRemain }

    public struct HoverInfo
    {
        public HoverKind Kind;
        public string RoomId;         // Room
        public int NextFloor;         // Roof
        public int Cost;              // Roof
        public Domain.FearTag ShopMain;
        public int ShopPrice;
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