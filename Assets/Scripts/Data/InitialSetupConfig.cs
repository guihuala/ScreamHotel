using System.Collections.Generic;
using UnityEngine;
using ScreamHotel.Domain;

[CreateAssetMenu(menuName="ScreamHotel/InitialSetup", fileName="InitialSetup")]
public class InitialSetupConfig : ScriptableObject
{
    [Header("Economy")]
    public int startGold = 300;

    [Header("Rooms")]
    public int startRoomCount = 2;      // 起始建几间 Lv1
    public bool giveDemoLv3 = true;     // 是否额外送一间 Lv3（演示容量与房间标签）

    [Header("Starter Ghosts")]
    public List<FearTag> starterGhostMains = new() { FearTag.Darkness, FearTag.Blood };
    public float defaultBaseScare = 35f;
}