using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.UI;
using UnityEngine.Serialization;

namespace ScreamHotel.Presentation
{
    public class TrainingSlot : MonoBehaviour, IHoverInfoProvider, IDropZone
    {
        [Header("Slot Components")]
        public Transform ghostAnchor;
        public MeshRenderer slotIndicator;

        [Header("Colors")]
        public Color trainingColor  = new Color(0.3f, 0.3f, 1f, 1f); // 训练中（可选显示）

        [Header("VFX")]
        public ParticleSystem trainingVfx;
        
        [Header("Training Settings")]
        public int trainingTimeDays = 2; // 可配置的训练时长

        [Header("Slot Icons")]
        public GameObject canTrainIcon;  // 可训练图标
        public GameObject cantTrainIcon; // 不可训练图标

        private GameObject currentSlotIcon; // 存储当前显示的图标
        
        
        // 完成标识（空闲时一律不显示）
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
 
            if (canTrainIcon != null) canTrainIcon.SetActive(false);
            if (cantTrainIcon != null) cantTrainIcon.SetActive(false);
        }

        // === 状态管理 ===
        public void SetEmptyState()
        {
            _ghostId = null;
            _slotState = GhostState.Idle;
            _trainingDays = 0;
            _isHovering = false;

            // 空闲时不显示完成标志
            if (completedMark) completedMark.SetActive(false);

            if (trainingVfx) trainingVfx.Stop();
        }

        public void StartTraining(string ghostId, FearTag tag)
        {
            _ghostId = ghostId;
            _slotState = GhostState.Training;
            _trainingDays = 0;
            _pendingTag = tag;              // 训练中只保存，先不写到 ghost.Sub

            if (completedMark) completedMark.SetActive(false); // 训练期间隐藏完成标记
            if (trainingVfx) trainingVfx.Play();               // 训练中持续释放粒子特效

            UpdateVisuals();
            UpdateTrainingDisplay();
        }

        public void CompleteTraining()
        {
            _slotState = GhostState.Idle;
            if (trainingVfx) trainingVfx.Stop();

            // 通知训练区：“此槽完成了”
            _trainingZone?.OnSlotTrainingComplete(_ghostId, this);

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
            UpdateVisuals();
        }

        // === 视觉更新 ===
        private void UpdateVisuals()
        {
            if (!slotIndicator) return;

            if (IsTraining)
            {
                // 训练中：保持粒子播放，颜色可作为辅助提示（若不想显示颜色，可直接禁用指示器）
                slotIndicator.material.color = trainingColor;
                if (trainingVfx && !trainingVfx.isPlaying) trainingVfx.Play();
                return;
            }

            if (IsOccupied)
            {
                return;
            }

            // 空闲且未悬停：默认不显示
            if (!_isHovering)
            {
                slotIndicator.material.color = Color.white;
            }
        }
        
        void OnValidate()
        {
            // 保障 ghostAnchor 有效
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
            
            if (canTrainIcon)  canTrainIcon.SetActive(canAccept);
            if (cantTrainIcon) cantTrainIcon.SetActive(!canAccept);
        }

        public void ClearHoverFeedback()
        {
            _isHovering = false;

            // 两个都关
            if (canTrainIcon)  canTrainIcon.SetActive(false);
            if (cantTrainIcon) cantTrainIcon.SetActive(false);
        }

        private void ShowCantAccept()
        {
            _isHovering = true;
            if (slotIndicator)
            {
                slotIndicator.enabled = true;
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
            _slotState = GhostState.Idle;   // 占位但尚未进入训练
            _trainingDays = 0;

            anchor = ghostAnchor;
            
            if (slotIndicator)
            {
                slotIndicator.enabled = true;
            }

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