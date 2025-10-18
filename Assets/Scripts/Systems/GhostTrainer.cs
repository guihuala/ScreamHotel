using ScreamHotel.Core;
using UnityEngine;
using ScreamHotel.Domain;

public class GhostTrainer : MonoBehaviour
{
    private Game _game;
    
    public void AdvanceOneDay()
    {
        foreach (var zone in FindObjectsOfType<TrainingRoomZone>())
            zone.AdvanceTrainingDayForAllSlots();
    }
}