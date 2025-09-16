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
        public MeshRenderer plate;         // 地板渲染器高亮用
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
            if (game == null) game = FindObjectOfType<ScreamHotel.Core.Game>();
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
