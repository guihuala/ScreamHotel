using UnityEngine;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    public class DraggableGuest : DraggableEntityBase<GuestView>
    {
        protected override bool IsGhost => false;
        protected override float DropMoveDuration => 0.12f;

        public void SetGuestId(string id) => SetEntityId(id); // 兼容旧接口

        protected override void UnassignEntity(AssignmentSystem assign, string id)
        {
            assign?.UnassignGuest(id);
        }
    }
}