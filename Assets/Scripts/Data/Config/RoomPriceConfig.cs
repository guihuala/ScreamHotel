using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/RoomPrice", fileName = "RoomPrice__")]
    public class RoomPriceConfig : ScriptableObject
    {
        public string id;
        public int buyCost = 200;
        public int upgradeToLv2 = 150;
        public int upgradeToLv3 = 300;
        public int capacityLv1 = 1;
        public int capacityLv3 = 2;
        public bool lv2HasTag = true;
        public bool lv3HasTag = true;
    }
}
