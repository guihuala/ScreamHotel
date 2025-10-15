using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.UI;

namespace ScreamHotel.Presentation
{
    public class TrainingSlot : MonoBehaviour, IHoverInfoProvider, IDropZone
    {
        [Header("Slot Components")]
        public Transform ghostAnchor;
        public MeshRenderer slotIndicator;

        [Header("Colors")]
        public Color emptyColor    = new Color(0.3f, 1f, 0.3f, 1f);
        public Color occupiedColor = new Color(1f, 0.3f, 0.3f, 1f);
        public Color trainingColor = new Color(0.3f, 0.3f, 1f, 1f);

        [Header("VFX")]
        public ParticleSystem trainingVfx;
        
        private TrainingRoomZone _trainingZone;
        private string _ghostId;
        private GhostState _slotState = GhostState.Idle;
        private int _trainingDays = 0;
        private bool _isHovering = false;

        public bool IsOccupied => !string.IsNullOrEmpty(_ghostId);
        public bool IsTraining => _slotState == GhostState.Training;
        public string GhostId => _ghostId;

        public int RemainDays
        {
            get
            {
                if (!IsTraining) return 0;
                var game = FindObjectOfType<Game>();
                if (game == null) return 0;
                int trainingTime = game.World.Config.Rules.ghostTrainingTimeDays;
                return Mathf.Max(0, trainingTime - _trainingDays);
            }
        }

        void Awake()
        {
            _trainingZone = GetComponentInParent<TrainingRoomZone>();
            SetEmptyState();
        }

        void OnValidate()
        {
            // 保障 ghostAnchor 有效，避免(0,0,0) 导致“飞走”
            if (ghostAnchor == null)
            {
                var t = transform.Find("GhostAnchor");
                if (t == null)
                {
                    var go = new GameObject("GhostAnchor");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = Vector3.zero;
                    ghostAnchor = go.transform;
                }
                else ghostAnchor = t;
            }
        }

        // === 状态管理 ===
        public void SetEmptyState()
        {
            _ghostId = null;
            _slotState = GhostState.Idle;
            _trainingDays = 0;

            _isHovering = false;
            UpdateVisuals();
        }

        public void StartTraining(string ghostId, FearTag tag)
        {
            _ghostId = ghostId;
            _slotState = GhostState.Training;
            _trainingDays = 0;

            if (trainingVfx) trainingVfx.Play();
            UpdateVisuals();
        }

        public void AdvanceTrainingDay()
        {
            if (!IsTraining) return;

            _trainingDays++;
            var game = FindObjectOfType<Game>();
            if (game != null)
            {
                int trainingTime = game.World.Config.Rules.ghostTrainingTimeDays;
                if (_trainingDays >= trainingTime) CompleteTraining();
            }
        }

        private void CompleteTraining()
        {
            _slotState = GhostState.Idle;
            if (trainingVfx) trainingVfx.Stop();
            _trainingZone?.OnSlotTrainingComplete(_ghostId);
            SetEmptyState();
        }

        // === 视觉（仅拖拽中点亮） ===
        private void UpdateVisuals()
        {
            if (!slotIndicator) return;
            
            slotIndicator.material.color =
                IsTraining ? trainingColor :
                IsOccupied ? occupiedColor : emptyColor;
        }

        // === 拖拽反馈 ===
        public void ShowHoverFeedback(string ghostId)
        {
            _isHovering = true;

            var game = FindObjectOfType<Game>();
            var ghost = game ? game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId) : null;
            bool canAccept = ghost != null && !IsOccupied && ghost.State != GhostState.Training;

            slotIndicator.enabled = true;
            slotIndicator.material.color = canAccept ? emptyColor : occupiedColor;
        }

        public void ClearHoverFeedback()
        {
            _isHovering = false;
            UpdateVisuals();
        }

        private void ShowCantAccept()
        {
            _isHovering = true;
            if (slotIndicator)
            {
                slotIndicator.enabled = true;
                slotIndicator.material.color = occupiedColor;
            }
        }

        // === 业务校验 / 投放（立即占位） ===
        public bool CanAccept(string ghostId)
        {
            if (string.IsNullOrEmpty(ghostId)) return false;

            var game = FindObjectOfType<Game>();
            var ghost = game ? game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId) : null;
            return !IsOccupied && ghost != null && ghost.State != GhostState.Training;
        }
        
        // === IHoverInfoProvider ===
        public HoverInfo GetHoverInfo() => new HoverInfo
        {
            Kind = HoverKind.TrainingRoom,
        };

        // === IDropZone ===
        bool IDropZone.CanAccept(string id, bool isGhost)
        {
            if (!isGhost) return false;
            return CanAccept(id);
        }

        bool IDropZone.TryDrop(string id, bool isGhost, out Transform targetAnchor)
        {
            targetAnchor = null;
            if (!isGhost) return false;
            if (!TryDrop(id, out targetAnchor)) return false;

            var zone = GetComponentInParent<TrainingRoomZone>();
            if (zone != null) zone.OpenPickFearUI(id, this);
            return true;
        }
        
        public bool TryDrop(string ghostId, out Transform anchor)
        {
            anchor = null;
            if (!CanAccept(ghostId)) return false;

            _ghostId = ghostId;
            _slotState = GhostState.Idle;
            _trainingDays = 0;

            anchor = ghostAnchor;
    
            UpdateVisuals();
            return true;
        }
        
        void IDropZone.ShowHoverFeedback(string id, bool isGhost)
        {
            if (!isGhost) { ShowCantAccept(); return; }
            ShowHoverFeedback(id);
        }

        void IDropZone.ClearFeedback() => ClearHoverFeedback();
    }
}
