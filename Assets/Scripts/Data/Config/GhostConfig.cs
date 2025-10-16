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

        [Header("Appearance (Optional)")]
        public GameObject prefabOverride;
    }
}