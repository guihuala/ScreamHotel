// GuestTypeConfig.cs
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
        public List<FearTag> immunities;
        public float barMax = 100f;
        [Range(0.5f, 0.99f)] public float requiredPercent = 0.8f;
        public int baseFee = 100;

        [Header("Appearance")]
        public GameObject prefabOverride;
        public Sprite portrait;
        [TextArea(3, 6)] public string intro;
        public List<Sprite> gallery;

        [Header("Spine (UI)")] [Tooltip("Spine-Unity 的 SkeletonDataAsset（UI用），若配置则在面板用Spine替代大图显示")]
        public SkeletonDataAsset spineUIData;
        [Tooltip("默认使用的Skin，可留空使用默认skin")] public string spineDefaultSkin = "";
        [Tooltip("默认播放的动画名，例如 idle / loop 等")] public string spineDefaultAnimation = "idle";
        [Tooltip("默认动画是否循环")] public bool spineDefaultLoop = true;
    }
}