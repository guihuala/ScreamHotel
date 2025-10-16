using System.Linq;
using ScreamHotel.Core;
using UnityEngine;
using ScreamHotel.Domain;

public class GhostTrainer : MonoBehaviour
{
    private World _world;
    private Game _game;

    public void Initialize(World world) { _world = world; }
    
    public void AdvanceOneDay()
    {
        // 改为通知所有训练槽位推进天数
        foreach (var zone in FindObjectsOfType<TrainingRoomZone>())
            zone.AdvanceTrainingDayForAllSlots();
    }
}