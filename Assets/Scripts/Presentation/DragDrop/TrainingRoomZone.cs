using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.Presentation;
using ScreamHotel.UI;

public class TrainingRoomZone : MonoBehaviour
{
    private Game _game;
    
    private readonly List<TrainingSlot> _slots = new List<TrainingSlot>();
    private readonly Dictionary<string, TrainingSlot> _ghostSlotMap = new Dictionary<string, TrainingSlot>();

    void Awake()
    {
        _game = FindObjectOfType<Game>();
        _slots.Clear();
        _slots.AddRange(GetComponentsInChildren<TrainingSlot>());
        
        // 初始化所有槽位
        foreach (var slot in _slots)
        {
            slot.Initialize(_game);
        }
        
        _ghostSlotMap.Clear();
    }

    // === 推进所有槽位的训练天数 ===
    public void AdvanceTrainingDayForAllSlots()
    {
        foreach (var slot in _slots)
        {
            if (slot.IsTraining)
            {
                slot.AdvanceTrainingDay();
            }
        }
        
        // 刷新训练室 UI 的"剩余天数"
        foreach (var zone in FindObjectsOfType<TrainingRoomZone>())
            zone.OnTrainingDayAdvanced();
    }

    // === 选择恐惧属性 UI ===
    public void OpenPickFearUI(string ghostId, TrainingSlot slot)
    {
        var hoverController = FindObjectOfType<HoverUIController>();
        if (hoverController != null && !hoverController.IsPickFearPanelActive())
        {
            hoverController.OpenPickFearPanel(ghostId, slot.transform, GetSlotIndex(slot), OnFearTagSelected);
        }
    }

    private int GetSlotIndex(TrainingSlot slot) => _slots.IndexOf(slot);
    private TrainingSlot GetSlotByIndex(int index) =>
        (index >= 0 && index < _slots.Count) ? _slots[index] : null;

    private void OnFearTagSelected(string ghostId, FearTag tag, int slotIndex)
    {
        var slot = GetSlotByIndex(slotIndex);
        if (slot != null)
            StartTraining(ghostId, tag, slot);
    }

    // === 启动训练 ===
    private void StartTraining(string ghostId, FearTag tag, TrainingSlot slot)
    {
        var ghost = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (ghost == null) return;

        // 设置鬼魂状态
        ghost.State = GhostState.Training;
        ghost.TrainingDays = 0;
        ghost.Sub = tag;

        // 启动槽位训练
        slot.StartTraining(ghostId, tag);
        _ghostSlotMap[ghostId] = slot;

        Debug.Log($"Started training for ghost: {ghostId} in slot");
    }

    public void OnTrainingDayAdvanced()
    {
        // 通知所有槽位更新UI显示
        foreach (var slot in _slots)
            if (slot.IsTraining) slot.UpdateTrainingDisplay();
    }

    public void OnSlotTrainingComplete(string ghostId, TrainingSlot slot)
    {
        if (_ghostSlotMap.ContainsKey(ghostId))
            _ghostSlotMap.Remove(ghostId);

        var ghost = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (ghost != null)
        {
            ghost.State = GhostState.Idle;
            Debug.Log($"Training complete for ghost: {ghostId}");
        }

        // 通知UI更新
        slot.CompleteTraining();
        
        // TODO: 训练完成事件
        // EventBus.Raise(new GhostTrainedEvent(ghost.Id, ghost.Sub ?? FearTag.Darkness));
    }
}