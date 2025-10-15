using UnityEngine;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    public class DraggablePawn : DraggableEntityBase<PawnView>
    {
        protected override bool IsGhost => true;
        protected override float DropMoveDuration => 0.08f;

        public void SetGhostId(string id) => SetEntityId(id); // 兼容旧接口

        // 训练槽位固定时禁止拖拽（子类特有）
        protected override bool IsExternallyLocked()
        {
            if (string.IsNullOrEmpty(entityId)) return false;
            var slots = FindObjectsOfType<TrainingSlot>(true);
            foreach (var s in slots)
                if (s && s.IsOccupied && s.GhostId == entityId) return true;
            return false;
        }

        protected override void UnassignEntity(AssignmentSystem assign, string id)
        {
            assign?.UnassignGhost(id);
        }
    }
}