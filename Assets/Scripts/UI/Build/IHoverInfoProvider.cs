using UnityEngine;

namespace ScreamHotel.UI
{
    public enum HoverKind { None, Room, Roof, ShopSlot }

    public struct HoverInfo
    {
        public HoverKind Kind;
        public string RoomId;         // Room
        public int NextFloor;         // Roof
        public int Cost;              // Roof
        public int ShopSlotIndex;     // Shop
        public Domain.FearTag ShopMain;
        public int ShopPrice;
        public Vector3 WorldPosition; // 用于定位3D/世界空间UI
    }

    public interface IHoverInfoProvider
    {
        HoverInfo GetHoverInfo();
    }

    public interface IClickActionProvider
    {
        // 返回 true 表示消费了点击（比如购买成功/建造成功）
        bool TryClick(Core.Game game);
    }
}