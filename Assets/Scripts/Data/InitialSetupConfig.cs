using System.Collections.Generic;
using UnityEngine;
using ScreamHotel.Domain;

[CreateAssetMenu(menuName="ScreamHotel/InitialSetup", fileName="InitialSetup")]
public class InitialSetupConfig : ScriptableObject
{
    [Header("Economy")]
    public int startGold = 300;

    [Header("Rooms")]
    public int startRoomCount = 8;      // 起始建几间 Lv1

    [Header("Starter Ghosts")]
    public List<FearTag> starterGhostMains = new() { FearTag.Darkness, FearTag.Blood };
}