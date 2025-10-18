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
            slot.Initialize(_game);

        _ghostSlotMap.Clear();

        Debug.Log($"[TrainingRoomZone] Awake: Found {_slots.Count} training slots");
    }

    // === 推进所有槽位的训练天数（由 GhostTrainer 调用） ===
    public void AdvanceTrainingDayForAllSlots()
    {
        int trainingSlotsCount = 0;
        foreach (var slot in _slots)
        {
            if (slot.IsTraining)
            {
                trainingSlotsCount++;
                slot.AdvanceTrainingDay();
            }
        }

        // 刷新训练室 UI 的"剩余天数"
        foreach (var zone in FindObjectsOfType<TrainingRoomZone>())
            zone.OnTrainingDayAdvanced();

        Debug.Log($"[TrainingRoomZone] Advanced {trainingSlotsCount} training slots");
    }

    // === 选择副属性 UI ===
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
        else
            Debug.LogError($"[TrainingRoomZone] Failed to find slot at index {slotIndex}");
    }

    // === 启动训练 ===
    private void StartTraining(string ghostId, FearTag tag, TrainingSlot slot)
    {
        var ghost = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (ghost == null)
        {
            Debug.LogError($"[TrainingRoomZone] StartTraining: Ghost {ghostId} not found!");
            return;
        }

        // 设置鬼魂状态（开始训练），计时清零；注意：不在此处写 Sub
        ghost.State        = GhostState.Training;
        ghost.TrainingDays = 0;

        // 槽位进入训练，保存“待学习”的副属性
        slot.StartTraining(ghostId, tag);
        _ghostSlotMap[ghostId] = slot;

        Debug.Log($"[TrainingRoomZone] Started training for ghost: {ghostId} in slot, tag: {tag}");
    }

    public void OnTrainingDayAdvanced()
    {
        // 刷新所有槽位 UI 的“剩余天数”
        foreach (var slot in _slots)
            if (slot.IsTraining)
                slot.UpdateTrainingDisplay();
    }

    /// <summary>
    /// 槽位“到达完成条件”时回调（由 TrainingSlot.AdvanceTrainingDay 触发）
    /// 仅在此处授予副属性；不再回调 slot.CompleteTraining()，避免递归。
    /// </summary>
    public void OnSlotTrainingComplete(string ghostId, TrainingSlot slot)
    {
        if (_ghostSlotMap.ContainsKey(ghostId))
            _ghostSlotMap.Remove(ghostId);

        var ghost = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (ghost != null)
        {
            // 完成时授予副属性，并解锁
            ghost.Sub   = slot != null ? slot.PendingTag : ghost.Sub;
            ghost.State = GhostState.Idle;

            Debug.Log($"[TrainingRoomZone] Completed: ghost={ghostId}, Sub={ghost.Sub}, State=Idle");
        }
        else
        {
            Debug.LogError($"[TrainingRoomZone] Ghost {ghostId} not found when completing training");
        }
        // 槽位本地完成收尾由 TrainingSlot 内部执行（停VFX、显示完成标识、但不清空占位）
    }
}
