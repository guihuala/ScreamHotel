using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.UI;
using TMPro;

namespace ScreamHotel.Presentation
{
    public class TrainingSlot : MonoBehaviour, IHoverInfoProvider // 实现悬停接口
    {
        [Header("Slot Components")]
        public Transform ghostAnchor;
        public MeshRenderer slotIndicator;

        [Header("Colors")]
        public Color emptyColor = new Color(0.3f, 1f, 0.3f, 1f);
        public Color occupiedColor = new Color(1f, 0.3f, 0.3f, 1f);
        public Color trainingColor = new Color(0.3f, 0.3f, 1f, 1f);

        [Header("VFX")]
        public ParticleSystem trainingVfx;

        private TrainingRoomZone _trainingZone;
        private string _ghostId;
        private GhostState _slotState = GhostState.Idle;
        private int _trainingDays = 0;
        private Color _originalColor;

        public bool IsOccupied => !string.IsNullOrEmpty(_ghostId);
        public bool IsTraining => _slotState == GhostState.Training;
        public string GhostId => _ghostId;
        public Transform Anchor => ghostAnchor;

        // 获取剩余天数（供HoverUIController使用）
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
            if (slotIndicator != null)
            {
                _originalColor = slotIndicator.material.color;
            }
            
            SetEmptyState();
        }

        // === 状态管理 ===
        public void SetEmptyState()
        {
            _ghostId = null;
            _slotState = GhostState.Idle;
            _trainingDays = 0;
            
            UpdateVisuals();
        }

        public void StartTraining(string ghostId, FearTag tag)
        {
            _ghostId = ghostId;
            _slotState = GhostState.Training;
            _trainingDays = 0;

            // 开始训练视觉特效
            if (trainingVfx != null)
            {
                trainingVfx.Play();
            }
            
            UpdateVisuals();
        }

        public void AdvanceTrainingDay()
        {
            if (!IsTraining) return;
            
            _trainingDays++;
            
            // 检查训练是否完成
            var game = FindObjectOfType<Game>();
            if (game != null)
            {
                int trainingTime = game.World.Config.Rules.ghostTrainingTimeDays;
                if (_trainingDays >= trainingTime)
                {
                    CompleteTraining();
                }
            }
        }

        private void CompleteTraining()
        {
            _slotState = GhostState.Idle;
            
            // 停止训练特效
            if (trainingVfx != null)
            {
                trainingVfx.Stop();
            }
            
            // 通知训练室
            _trainingZone?.OnSlotTrainingComplete(_ghostId);
            
            // 清空槽位
            SetEmptyState();
        }

        // === 视觉更新 ===
        private void UpdateVisuals()
        {
            if (slotIndicator != null)
            {
                if (IsTraining)
                {
                    slotIndicator.material.color = trainingColor;
                }
                else if (IsOccupied)
                {
                    slotIndicator.material.color = occupiedColor;
                }
                else
                {
                    slotIndicator.material.color = emptyColor;
                }
            }
        }

        // === 拖拽反馈 ===
        public void ShowHoverFeedback(string ghostId)
        {
            if (slotIndicator == null) return;
            
            var game = FindObjectOfType<Game>();
            if (game == null) return;

            var ghost = game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
            if (ghost == null) return;

            bool canAccept = !IsOccupied && ghost.State != GhostState.Training;
            slotIndicator.material.color = canAccept ? emptyColor : occupiedColor;
        }

        public void ClearHoverFeedback()
        {
            if (slotIndicator != null)
            {
                UpdateVisuals();
            }
        }

        public bool CanAccept(string ghostId)
        {
            if (string.IsNullOrEmpty(ghostId)) return false;

            var game = FindObjectOfType<Game>();
            if (game == null) return false;

            var ghost = game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
            if (ghost == null) return false;

            // 槽位必须为空，且鬼魂不在训练中
            return !IsOccupied && ghost.State != GhostState.Training;
        }

        public bool TryDrop(string ghostId, out Transform anchor)
        {
            anchor = null;
            
            if (!CanAccept(ghostId)) return false;
            
            anchor = ghostAnchor;
            return true;
        }

        // === 悬停信息提供 ===
        public HoverInfo GetHoverInfo() => new HoverInfo
        {
            Kind = HoverKind.TrainingRoom,
            WorldPosition = transform.position
        };
    }
}