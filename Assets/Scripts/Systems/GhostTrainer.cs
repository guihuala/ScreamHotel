using ScreamHotel.Core;
using UnityEngine;
using ScreamHotel.Domain;

public class GhostTrainer : MonoBehaviour
{
    private World _world;
    
    public void Initialize(World world)
    {
        _world = world;
    }

    public void StartTraining(Ghost ghost)
    {
        if (ghost.State == GhostState.Training) return; // 如果已经在训练，就不再启动训练

        ghost.State = GhostState.Training;
        ghost.TrainingDays = 0; // 重置训练天数
        Debug.Log($"Training started for ghost: {ghost.Id}");
    }

    public void Update()
    {
        // 训练进度
        foreach (var ghost in _world.Ghosts)
        {
            if (ghost.State == GhostState.Training)
            {
                ghost.TrainingDays++;

                // 如果训练完成（经过2天）
                if (ghost.TrainingDays >= _world.Config.Rules.ghostTrainingTimeDays)
                {
                    CompleteTraining(ghost);
                }
            }
        }
    }

    private void CompleteTraining(Ghost ghost)
    {
        ghost.State = GhostState.Idle;  // 完成训练后回到空闲状态
        Debug.Log($"Training complete for ghost: {ghost.Id}");

        // 给鬼怪增加一个额外的恐惧属性
        // todo.允许玩家选择属性
        FearTag newFear = (FearTag)Random.Range(0, System.Enum.GetValues(typeof(FearTag)).Length);
        ghost.Sub = newFear;

        // 触发事件，鬼怪的训练已完成
        // EventBus.Raise(new GhostTrainedEvent(ghost.Id, newFear));
    }
}