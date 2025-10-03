using System.Collections;
using System.Collections.Generic;
using ScreamHotel.Domain;
using UnityEngine;

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
        
        [Header("Appearance")]
        public Color colorTint = Color.white;        // 基础色调
        public Material overrideMaterial;            // 材质
        public GameObject prefabOverride;            // 替换模型
        public Sprite icon;                          // UI显示用图标
    }
}