using System.Linq;
using ScreamHotel.Core;
using UnityEngine;
using ScreamHotel.Domain;

public class GhostTrainer : MonoBehaviour
{
    private World _world;
    private Game _game;

    public void Initialize(World world) { _world = world; }
    
    public void StartTraining(string ghostId, FearTag tag)
    {
        var ghost = _game.World.Ghosts.FirstOrDefault(x => x.Id == ghostId);
        if (ghost == null) return;
    
        ghost.State = GhostState.Training;
        ghost.TrainingDays = 0;
        ghost.Sub = tag; // 直接设置选择的属性
    }
    
    // 由 Game 在“进入新的一天”时调用
    public void AdvanceOneDay()
    {
        foreach (var ghost in _world.Ghosts)
        {
            if (ghost.State != GhostState.Training) continue;
            ghost.TrainingDays++;

            if (ghost.TrainingDays >= _world.Config.Rules.ghostTrainingTimeDays)
            {
                CompleteTraining(ghost);
            }
        }

        // 刷新训练室 UI 的“剩余天数”
        foreach (var zone in FindObjectsOfType<TrainingRoomZone>())
            zone.OnTrainingDayAdvanced();
    }

    private void CompleteTraining(Ghost ghost)
    {
        ghost.State = GhostState.Idle;
        Debug.Log($"Training complete for ghost: {ghost.Id}");

        // 如果你想在完成时才真正赋予 Sub，这里做：
        // ghost.Sub = ghost.Sub ?? PickedTagCache[ghost.Id];

        // 通知训练室把特效移除、槽位释放
        foreach (var zone in FindObjectsOfType<TrainingRoomZone>())
            zone.OnTrainingComplete(ghost);

        // TODO: 训练完成事件
        // EventBus.Raise(new GhostTrainedEvent(ghost.Id, ghost.Sub ?? FearTag.Darkness));
    }
}