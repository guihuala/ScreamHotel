using System.Collections;
using System.Collections.Generic;
using ScreamHotel.Domain;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/Ghost", fileName = "Ghost__")]
    public class GhostConfig : ScriptableObject
    {
        public string id;
        public FearTag main;
        public FearTag subRestriction;
        public float baseScare = 30f;
        public int recruitCost = 100;
    }
}