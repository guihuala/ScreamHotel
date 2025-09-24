// GuestView.cs（新建：Assets/Scripts/Presentation/GuestView.cs）
using UnityEngine;
using ScreamHotel.Domain;

namespace ScreamHotel.Presentation
{
    public class GuestView : MonoBehaviour
    {
        public string guestId;
        public void BindGuest(string id) { guestId = id; }
        public void SnapTo(Vector3 pos) { transform.position = pos; }
        public void MoveTo(Transform target, float t=0.3f)
        {
            StopAllCoroutines();
            StartCoroutine(MoveCo(target.position, t));
        }
        private System.Collections.IEnumerator MoveCo(Vector3 dest, float dur)
        {
            var from = transform.position; float s=0;
            while (s<1f){ s+=Time.deltaTime/Mathf.Max(0.001f,dur); transform.position=Vector3.Lerp(from,dest,Mathf.SmoothStep(0,1,s)); yield return null; }
            transform.position = dest;
        }
    }
}