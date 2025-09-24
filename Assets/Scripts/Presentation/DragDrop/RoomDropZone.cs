using UnityEngine;
using System.Linq;
using ScreamHotel.Domain;
using ScreamHotel.Systems;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class RoomDropZone : MonoBehaviour
    {
        [Header("Binding")]
        public string roomId;
        public MeshRenderer plate;
        public Color canColor   = new Color(0.3f, 1f, 0.3f, 1f);
        public Color fullColor  = new Color(1f, 0.3f, 0.3f, 1f);
        public Color baseColor  = new Color(1f, 1f, 1f, 1f);

        private Core.Game game;

        private AssignmentSystem _assign => GetSystem<AssignmentSystem>(game, "_assignmentSystem");
        private Color _origColor;

        void Awake()
        {
            if (plate != null)
            {
                _origColor = plate.sharedMaterial.color;
                baseColor = _origColor;
            }
            if (game == null) game = FindObjectOfType<Core.Game>();
        }

        public bool CanAccept(string ghostId)
        {
            var w = game.World;
            var r = w.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null) return false;
            if (r.AssignedGhostIds.Count >= r.Capacity) return false;

            var g = w.Ghosts.FirstOrDefault(x => x.Id == ghostId);
            if (g == null) return false;
            if (g.State is GhostState.Resting or GhostState.Training or GhostState.Injured) return false;

            return true;
        }

        public bool TryDrop(string ghostId, out Transform targetAnchor)
        {
            targetAnchor = null;
            var w = game.World;
            var r = w.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null) return false;

            if (!CanAccept(ghostId)) { Flash(fullColor); return false; }

            if (_assign.TryAssignGhostToRoom(ghostId, roomId))
            {
                // 计算这只鬼在本房的索引
                var index = r.AssignedGhostIds.IndexOf(ghostId);
                var rv = GetComponent<RoomView>();
                targetAnchor = rv != null ? rv.GetAnchor(index) : transform;
                Flash(canColor);
                return true;
            }
            Flash(fullColor);
            return false;
        }
        
        // RoomDropZone.cs（追加）
        public void ShowHoverFeedbackGuest()
        {
            // 也可以区分颜色；这里简单沿用可投放的绿色/红色规则
            if (plate == null) return;
            plate.material.color = canColor;
        }

        public bool TryDropGuest(string guestId, out Transform targetAnchor)
        {
            targetAnchor = null;
            var w = game.World;
            var r = w.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null) return false;

            if (_assign.TryAssignGuestToRoom(guestId, roomId))
            {
                // 客人锚点：重用 RoomView 的“第二列锚点”或额外的 guestAnchors（这里拿 index = 已有客人数-1）
                var idx = Mathf.Max(0, r.AssignedGuestIds.IndexOf(guestId));
                var rv = GetComponent<RoomView>();
                targetAnchor = rv != null ? rv.GetGuestAnchor(idx) : transform;
                Flash(canColor);
                return true;
            }
            Flash(fullColor);
            return false;
        }


        public void ShowHoverFeedback(string ghostId)
        {
            if (plate == null) return;
            var c = CanAccept(ghostId) ? canColor : fullColor;
            plate.material.color = Color.Lerp(plate.material.color, c, 0.5f);
        }

        public void ClearFeedback()
        {
            if (plate == null) return;
            plate.material.color = _origColor;
        }

        private static T GetSystem<T>(object obj, string field) where T : class
        {
            var f = obj.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f?.GetValue(obj) as T;
        }

        private void Flash(Color c)
        {
            if (plate == null) return;
            plate.material.color = c;
            CancelInvoke(nameof(Revert));
            Invoke(nameof(Revert), 0.25f);
        }
        private void Revert()
        {
            if (plate != null) plate.material.color = _origColor;
        }
    }
}
