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
        
        [Header("Training Settings")]
        public int trainingTimeDays = 2; // 可配置的训练时长
        
        // 完成标识
        public GameObject completedMark;
        
        private FearTag _pendingTag = default;
        public FearTag PendingTag => _pendingTag;
        
        public bool IsCompleted => completedMark != null && completedMark.activeSelf;
        
        private TrainingRoomZone _trainingZone;
        private Game _game;
        private string _ghostId;
        private GhostState _slotState = GhostState.Idle;
        private int _trainingDays = 0;
        private bool _isHovering = false;

        public bool IsOccupied => !string.IsNullOrEmpty(_ghostId);
        public bool IsTraining => _slotState == GhostState.Training;
        public string GhostId => _ghostId;

        public void Initialize(Game game)
        {
            _game = game;
            SetEmptyState();
        }

        void Awake()
        {
            _trainingZone = GetComponentInParent<TrainingRoomZone>();
            if (_game == null)
                _game = FindObjectOfType<Game>();
        }

        // === 状态管理 ===
        public void SetEmptyState()
        {
            _ghostId = null;
            _slotState = GhostState.Idle;
            _trainingDays = 0;
            _isHovering = false;
            UpdateVisuals();
            
            if (trainingVfx) 
                trainingVfx.Stop();
        }

        public void StartTraining(string ghostId, FearTag tag)
        {
            _ghostId = ghostId;
            _slotState = GhostState.Training;
            _trainingDays = 0;
            _pendingTag = tag;              // ← 训练中只保存，先不写到 ghost.Sub

            if (completedMark) completedMark.SetActive(false); // 开始训练时隐藏完成标记
            if (trainingVfx) trainingVfx.Play();

            UpdateVisuals();
            UpdateTrainingDisplay();
        }

        public void CompleteTraining()
        {
            _slotState = GhostState.Idle;
            if (trainingVfx) trainingVfx.Stop();

            // 通知训练区：“此槽完成了”
            _trainingZone?.OnSlotTrainingComplete(_ghostId, this);

            // 在槽位上展示完成标识（鬼可被拖走）
            if (completedMark) completedMark.SetActive(true);

            // 槽位本身清空占用（让它可以继续接收新的鬼）
            SetEmptyState();
        }


        public void AdvanceTrainingDay()
        {
            if (!IsTraining) return;

            _trainingDays++;
            
            // 更新鬼魂数据
            var ghost = _game?.World.Ghosts.FirstOrDefault(x => x.Id == _ghostId);
            if (ghost != null)
            {
                ghost.TrainingDays = _trainingDays;
            }

            UpdateTrainingDisplay();

            // 检查训练是否完成
            if (_trainingDays >= trainingTimeDays)
            {
                CompleteTraining();
            }
        }

        public void UpdateTrainingDisplay()
        {
            // 更新UI显示，但不推进天数
            UpdateVisuals();
        }

        // === 视觉更新 ===
        private void UpdateVisuals()
        {
            if (!slotIndicator) return;
            
            slotIndicator.material.color =
                IsTraining ? trainingColor :
                IsOccupied ? occupiedColor : emptyColor;
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
            if (game == null || game.State != GameState.Day) return false;

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
            var game = FindObjectOfType<Game>();
            if (game == null || game.State != GameState.Day) return false;

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
