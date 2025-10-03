// GuestView.cs
using UnityEngine;
using ScreamHotel.Domain;
using ScreamHotel.Core;
using ScreamHotel.Data; // 引入以便查 Game

namespace ScreamHotel.Presentation
{
    public class GuestView : MonoBehaviour
    {
        public string guestId;

        [Header("Visual")]
        public MeshRenderer body;
        
        public void BindGuest(string id)
        {
            guestId = id;

            var game = FindObjectOfType<Game>();
            var g = game?.World?.Guests?.Find(x => x.Id == guestId);
            if (g == null) return;

            // 查找 GuestTypeConfig
            var config = game.dataManager.Database.GetGuestType(g.TypeId);
            if (config != null)
            {
                ApplyConfig(config);
            }
        }

        private void ApplyConfig(GuestTypeConfig config)
        {
            if (body != null)
            {
                if (config.overrideMaterial != null)
                    body.material = config.overrideMaterial;
                else
                    body.material.color = config.colorTint;
            }
            
            if (config.prefabOverride != null)
            {
                foreach (Transform child in transform) Destroy(child.gameObject);
                Instantiate(config.prefabOverride, transform);
            }

            // UI 显示用
            if (config.icon != null)
            {
                
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
