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
        _ghostSlotMap.Clear();
    }

    // === 选择恐惧属性 UI（供 slot 调用） ===
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
        var g = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (g == null) return;

        g.State = GhostState.Training;
        g.TrainingDays = 0;
        g.Sub = tag;

        slot.StartTraining(ghostId, tag);
        _ghostSlotMap[ghostId] = slot;

        var trainer = FindObjectOfType<GhostTrainer>();
        if (trainer) trainer.StartTraining(ghostId, tag);
    }

    public void OnTrainingDayAdvanced()
    {
        foreach (var slot in _slots)
            if (slot.IsTraining) slot.AdvanceTrainingDay();
    }

    public void OnSlotTrainingComplete(string ghostId)
    {
        if (_ghostSlotMap.ContainsKey(ghostId))
            _ghostSlotMap.Remove(ghostId);

        var ghost = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (ghost != null)
        {
            ghost.State = GhostState.Idle;
            var trainer = FindObjectOfType<GhostTrainer>();
            if (trainer)
            {
                // 可在此调用训练完成的通用逻辑
            }
        }
    }
}
