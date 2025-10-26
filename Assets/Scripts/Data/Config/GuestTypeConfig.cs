using System.Collections.Generic;
using ScreamHotel.Domain;
using UnityEngine;
using Spine.Unity;

namespace ScreamHotel.Data
{
    [CreateAssetMenu(menuName = "ScreamHotel/GuestType", fileName = "GuestType__")]
    public class GuestTypeConfig : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Behavior")]
        public List<FearTag> immunities; // 可用的免疫属性列表
        public int maxImmunitiesCount = 2; // 每个游客的最大免疫属性数量
        public float barMax = 100f;
        [Range(0.5f, 0.99f)] public float requiredPercent = 0.8f;
        public int baseFee = 100;

        [Header("Appearance")]
        public GameObject prefabOverride;
        public Sprite portrait;
        [TextArea(3, 6)] public string intro;

        [Header("Spine (UI)")]
        public SkeletonDataAsset spineUIData;
        [Tooltip("默认使用的Skin，可留空使用默认skin")] public string spineDefaultSkin = "";
        [Tooltip("默认播放的动画名，例如 idle / loop 等")] public string spineDefaultAnimation = "idle";
        [Tooltip("默认动画是否循环")] public bool spineDefaultLoop = true;
    }
}