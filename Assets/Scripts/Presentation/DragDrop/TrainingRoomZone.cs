using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.Presentation;
using ScreamHotel.UI;

public class TrainingRoomZone : MonoBehaviour, IDropZone
{
    private Game _game;
    private TrainingRoomView _view;
    
    private readonly List<TrainingSlot> _slots = new List<TrainingSlot>();
    private readonly Dictionary<string, TrainingSlot> _ghostSlotMap = new Dictionary<string, TrainingSlot>();

    void Awake()
    {
        _game = FindObjectOfType<Game>();
        _view = GetComponentInChildren<TrainingRoomView>();
        
        // 获取所有槽位
        _slots.Clear();
        _slots.AddRange(GetComponentsInChildren<TrainingSlot>());
        _ghostSlotMap.Clear();
    }

    // ==== 拖拽反馈 ====
    public void ShowHoverFeedback(string ghostId)
    {
        // 让所有槽位显示悬停反馈
        foreach (var slot in _slots)
        {
            slot.ShowHoverFeedback(ghostId);
        }
    }

    public void ShowHoverFeedback(string id, bool isGhost)
    {
        ShowHoverFeedback(id);
    }

    public void ClearFeedback()
    {
        // 清除所有槽位的悬停反馈
        foreach (var slot in _slots)
        {
            slot.ClearHoverFeedback();
        }
    }

    public bool TryDrop(string ghostId, bool isGhost, out Transform targetAnchor)
    {
        targetAnchor = null;
        
        if (!CanAccept(ghostId, isGhost))
        {
            _view?.Flash(_view.fullColor);
            return false;
        }

        // 查找第一个可用的槽位
        var availableSlot = _slots.FirstOrDefault(slot => slot.CanAccept(ghostId));
        if (availableSlot == null)
        {
            _view?.Flash(_view.fullColor);
            return false;
        }

        // 锁定槽位
        if (availableSlot.TryDrop(ghostId, out targetAnchor))
        {
            // 弹出"选择恐惧属性"的小面板
            OpenPickFearUI(ghostId, availableSlot);
            _view?.Flash(_view.canColor);
            return true;
        }

        return false;
    }

    public bool CanAccept(string ghostId, bool isGhost)
    {
        if (_game == null || string.IsNullOrEmpty(ghostId)) return false;

        var g = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (g == null) return false;

        // 训练中不可重复放置；检查是否有可用槽位
        if (g.State == GhostState.Training) return false;
        
        return _slots.Any(slot => slot.CanAccept(ghostId));
    }

    // === 选择恐惧属性 UI ===
    private void OpenPickFearUI(string ghostId, TrainingSlot slot)
    {
        var hoverController = FindObjectOfType<HoverUIController>();
        if (hoverController != null && !hoverController.IsPickFearPanelActive())
        {
            hoverController.OpenPickFearPanel(ghostId, GetSlotIndex(slot), OnFearTagSelected);
        }
        else
        {
            // 备用方案：直接创建面板
            var ui = new GameObject("PickFearPanel").AddComponent<PickFearPanel>();
            ui.transform.SetParent(transform, false);
            ui.Init(ghostId, GetSlotIndex(slot), OnFearTagSelected);
        }
    }

    private int GetSlotIndex(TrainingSlot slot)
    {
        return _slots.IndexOf(slot);
    }

    private TrainingSlot GetSlotByIndex(int index)
    {
        return (index >= 0 && index < _slots.Count) ? _slots[index] : null;
    }

    private void OnFearTagSelected(string ghostId, FearTag tag, int slotIndex)
    {
        var slot = GetSlotByIndex(slotIndex);
        if (slot != null)
        {
            StartTraining(ghostId, tag, slot);
        }
    }

    // === 启动训练 ===
    private void StartTraining(string ghostId, FearTag tag, TrainingSlot slot)
    {
        var g = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (g == null) return;

        // 更新鬼魂状态
        g.State = GhostState.Training;
        g.TrainingDays = 0;
        g.Sub = tag;

        // 启动槽位训练
        slot.StartTraining(ghostId, tag);
        
        // 记录映射关系
        _ghostSlotMap[ghostId] = slot;

        // 通知 GhostTrainer
        var trainer = FindObjectOfType<GhostTrainer>();
        if (trainer) trainer.StartTraining(ghostId, tag);
    }

    public void OnTrainingDayAdvanced()
    {
        // 让所有训练中的槽位推进一天
        foreach (var slot in _slots)
        {
            if (slot.IsTraining)
            {
                slot.AdvanceTrainingDay();
            }
        }
    }

    public void OnSlotTrainingComplete(string ghostId)
    {
        // 从映射中移除
        if (_ghostSlotMap.ContainsKey(ghostId))
        {
            _ghostSlotMap.Remove(ghostId);
        }

        // 通知训练完成
        var ghost = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (ghost != null)
        {
            ghost.State = GhostState.Idle;
            
            // 通知其他系统
            var trainer = FindObjectOfType<GhostTrainer>();
            if (trainer)
            {
                // 这里可以调用训练完成的通用方法
            }
        }

        // 闪一下表示完成
        _view?.Flash(_view.canColor);
    }

    // 兼容旧的接口（供GhostTrainer调用）
    public void OnTrainingComplete(Ghost ghost)
    {
        OnSlotTrainingComplete(ghost.Id);
    }
}