using UnityEngine;
using ScreamHotel.Domain;
using ScreamHotel.UI;

namespace ScreamHotel.Presentation
{
    public class TrainingRoomView : MonoBehaviour, IHoverInfoProvider
    {
        [Header("Visual")]
        public MeshRenderer plate;
        public Color canColor = new Color(0.3f, 1f, 0.3f, 1f);
        public Color fullColor = new Color(1f, 0.3f, 0.3f, 1f);

        private TrainingRoomZone _trainingZone;
        private Color _origColor;

        void Awake()
        {
            _trainingZone = GetComponentInParent<TrainingRoomZone>();
            if (plate) _origColor = plate.material.color;
        }

        // ==== 视觉反馈 ====
        public void ShowHoverFeedback(string ghostId)
        {
            if (plate == null) return;
            var c = _trainingZone.CanAccept(ghostId, true) ? canColor : fullColor;
            plate.material.color = Color.Lerp(plate.material.color, c, 0.5f);
        }

        public void ClearFeedback()
        {
            if (plate == null) return;
            plate.material.color = _origColor;
        }

        public void Flash(Color c)
        {
            if (!plate) return;
            plate.material.color = c;
            CancelInvoke(nameof(Revert)); 
            Invoke(nameof(Revert), 0.25f);
        }

        private void Revert()
        { 
            if (plate) plate.material.color = _origColor; 
        }

        public HoverInfo GetHoverInfo() => new HoverInfo
        {
            Kind = HoverKind.TrainingRoom,
            WorldPosition = transform.position
        };
    }
}