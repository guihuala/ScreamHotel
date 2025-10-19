using System.Collections.Generic;
using ScreamHotel.Domain;
using UnityEngine;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/GuestType", fileName = "GuestType__")]
    public class GuestTypeConfig : ScriptableObject
    {
        [Header("Identity")]
        public string id;                     // 配置ID（与 Guest.TypeId 对应）
        public string displayName;            // 显示名称（面板大标题）

        [Header("Behavior")]
        public List<FearTag> immunities;
        public float barMax = 100f;
        [Range(0.5f, 0.99f)] public float requiredPercent = 0.8f;
        public int baseFee = 100;

        [Header("Appearance")]
        public GameObject prefabOverride;     // 场景中替换模型
        public Sprite portrait;               // 头像（左侧大图 & 右侧缩略图）
        [TextArea(3, 6)] public string intro; // 简介（中部多行文案）
        public List<Sprite> gallery;          // 备用缩略图（如需在右侧显示不同姿态）
    }
}