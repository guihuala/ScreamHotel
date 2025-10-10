using UnityEngine;
using ScreamHotel.Domain;

namespace ScreamHotel.Presentation.Shop
{
    public class GhostIconView : MonoBehaviour
    {
        public Renderer bodyRenderer; // 绑定到一个简易 Mesh 的材质（如 Capsule/Quad）
        public Color colorFear; // 默认颜色
        public Color colorBlood;
        public Color colorShadow;
        public Color colorDoll;
        public Color colorPoison;

        public void Apply(FearTag tag)
        {
            if (!bodyRenderer) return;
            var c = tag switch
            {
                FearTag.Blood => colorBlood,
                FearTag.Darkness => colorShadow,
                FearTag.Gaze => colorDoll,
                FearTag.Noise => colorPoison,
                FearTag.Rot => colorFear,
                _ => colorFear
            };
            if (bodyRenderer.material.HasProperty("_Color"))
                bodyRenderer.material.color = c;
        }
    }
}