using UnityEngine;
using ScreamHotel.Domain;

namespace ScreamHotel.Presentation
{
    public class PawnView : MonoBehaviour
    {
        public string ghostId;
        public MeshRenderer body;

        public void BindGhost(Ghost g)
        {
            ghostId = g.Id;
            if (body != null) body.material.color = ColorFor(g.Main);
            name = $"GhostPawn_{g.Id}";

            // 获取 DraggablePawn 并设置 ghostId
            var draggable = GetComponentInParent<DraggablePawn>();
            if (draggable != null)
            {
                draggable.SetGhostId(ghostId);  // 在这里设置 ghostId
            }
        }

        public void SnapTo(Vector3 target)
        {
            transform.position = target;
        }

        public void MoveTo(Transform target, float dur = 0.5f)
        {
            StopAllCoroutines();
            StartCoroutine(MoveRoutine(target.position, dur));
        }

        System.Collections.IEnumerator MoveRoutine(Vector3 to, float dur)
        {
            var from = transform.position; float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, dur);
                transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            transform.position = to;
        }

        private Color ColorFor(FearTag tag)
        {
            switch (tag)
            {
                case FearTag.Darkness: return new Color(0.35f, 0.35f, 1f);
                case FearTag.Blood: return new Color(1f, 0.3f, 0.3f);
                case FearTag.Noise: return new Color(1f, 0.8f, 0.2f);
                case FearTag.Rot: return new Color(0.55f, 0.8f, 0.3f);
                case FearTag.Gaze: return new Color(0.8f, 0.5f, 1f);
                default: return Color.white;
            }
        }
    }
}