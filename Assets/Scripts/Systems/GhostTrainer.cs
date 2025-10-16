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
        foreach (var zone in FindObjectsOfType<TrainingRoomZone>())
            zone.AdvanceTrainingDayForAllSlots();
    }
}