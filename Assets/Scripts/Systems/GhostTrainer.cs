using ScreamHotel.Core;
using UnityEngine;
using ScreamHotel.Domain;

public class GhostTrainer : MonoBehaviour
{
    private World _world;

    public void Initialize(World world) { _world = world; }

    public void StartTraining(Ghost ghost)
    {
        if (ghost.State == GhostState.Training) return;
        ghost.State = GhostState.Training;
        ghost.TrainingDays = 0;
        Debug.Log($"Training started for ghost: {ghost.Id}");
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

        // TODO: 你也可以在这里 Raise 一个“训练完成事件”，用于弹提示/飘字
        // EventBus.Raise(new GhostTrainedEvent(ghost.Id, ghost.Sub ?? FearTag.Darkness));
    }
}