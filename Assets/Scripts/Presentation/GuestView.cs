using UnityEngine;
using ScreamHotel.Domain;

namespace ScreamHotel.Presentation
{
    public class GuestView : MonoBehaviour
    {
        public string guestId;
        
        public void BindGuest(string id)
        {
            guestId = id;
            Debug.Log($"GuestView 绑定了 ID: {guestId}");
            
            // 获取 DraggableGuest 并设置 guestId
            var draggable = GetComponentInParent<DraggableGuest>();
            if (draggable != null)
            {
                draggable.SetGuestId(guestId);  // 在这里设置 guestId
            }
        }

        public void SnapTo(Vector3 pos) { transform.position = pos; }
        public void MoveTo(Transform target, float t = 0.3f)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCo(target.position, t));
        }

        private System.Collections.IEnumerator MoveCo(Vector3 dest, float dur)
        {
            var from = transform.position;
            float s = 0;
            while (s < 1f)
            {
                s += Time.deltaTime / Mathf.Max(0.001f, dur);
                transform.position = Vector3.Lerp(from, dest, Mathf.SmoothStep(0, 1, s));
                yield return null;
            }
            transform.position = dest;
        }
    }
}