using UnityEngine;
using ScreamHotel.Domain;
using ScreamHotel.UI;

namespace ScreamHotel.Presentation
{
    public class TrainingRoomView : MonoBehaviour, IHoverInfoProvider
    {
        public HoverInfo GetHoverInfo() => new HoverInfo
        {
            Kind = HoverKind.TrainingRoom,
            WorldPosition = transform.position
        };
    }
}