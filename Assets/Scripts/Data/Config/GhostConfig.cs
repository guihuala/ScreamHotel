using System.Collections.Generic;
using ScreamHotel.Domain;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/Ghost", fileName = "Ghost__")]
    public class GhostConfig : ScriptableObject
    {
        [Header("Identity")]
        public string id;

        [Header("Fear Profile")]
        public FearTag main;
        public FearTag subRestriction;

        [Header("Appearance (Optional)")]
        public GameObject prefabOverride;            // 整体外观替换（模型）
    }
}