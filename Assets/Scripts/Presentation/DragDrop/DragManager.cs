using UnityEngine;

namespace ScreamHotel.Presentation
{
    /// <summary>
    /// 提供一个简单的地面层与平面碰撞体，供拖拽落点检测。
    /// </summary>
    public class DragManager : MonoBehaviour
    {
        public LayerMask groundMask;

        private void OnValidate()
        {
            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                
            }
        }
    }
}