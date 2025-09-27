using System.Collections;
using System.Collections.Generic;
using ScreamHotel.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/GuestType", fileName = "GuestType__")]
    public class GuestTypeConfig : ScriptableObject
    {
        public string id;
        public List<FearTag> immunities;
        public float barMax = 100f;
        [Range(0.5f, 0.99f)] public float requiredPercent = 0.8f;
        public int baseFee = 100;
        [Header("Counter Rules")] public float counterChance = 0f;
    }
}