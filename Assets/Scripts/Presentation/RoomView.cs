using UnityEngine;
using ScreamHotel.Domain;

namespace ScreamHotel.Presentation
{
    public class RoomView : MonoBehaviour
    {
        [Header("Binding")]
        public string roomId;
        public MeshRenderer plate;       // 地块的渲染器（用于改色）
        public Transform[] ghostAnchors; // 鬼锚点，长度=房间容量

        [Header("UI (Optional)")]
        public TextMesh label;           // 简易 3D 文本（可换 TMP）

        private Color _baseColor;
        private float _pulseT; private Color _targetColor;

        void Awake()
        {
            if (plate != null) _baseColor = plate.sharedMaterial.color;
            RefreshLabel(null);
        }

        public void Bind(Room room)
        {
            roomId = room.Id;
            EnsureAnchors(room.Capacity);
            Refresh(room);
        }

        public void Refresh(Room room)
        {
            // 标签：Room_01 Lv2 [Darkness] Cap:2
            var tag = room.RoomTag.HasValue ? room.RoomTag.Value.ToString() : "-";
            if (label) label.text = $"{room.Id}  Lv{room.Level}  [{tag}]  Cap:{room.Capacity}";
            EnsureAnchors(room.Capacity);
        }

        public void PulseSuccess()  => StartPulse(new Color(0.3f, 1f, 0.3f));
        public void PulseFail()     => StartPulse(new Color(1f, 0.9f, 0.3f));
        public void PulseCounter()  => StartPulse(new Color(1f, 0.3f, 0.3f));

        void StartPulse(Color c) { _targetColor = c; _pulseT = 1f; }

        void Update()
        {
            if (_pulseT > 0f && plate != null)
            {
                _pulseT -= Time.deltaTime * 1.8f;
                var t = Mathf.Clamp01(_pulseT);
                plate.material.color = Color.Lerp(_baseColor, _targetColor, Mathf.SmoothStep(0,1,t));
                if (_pulseT <= 0f) plate.material.color = _baseColor;
            }
        }

        public Transform GetAnchor(int index) =>
            (ghostAnchors != null && index >= 0 && index < ghostAnchors.Length) ? ghostAnchors[index] : transform;

        private void EnsureAnchors(int capacity)
        {
            if (ghostAnchors == null || ghostAnchors.Length < capacity)
            {
                ghostAnchors = new Transform[Mathf.Max(capacity, 1)];
                for (int i = 0; i < ghostAnchors.Length; i++)
                {
                    var child = transform.Find($"Anchor{i}") ?? new GameObject($"Anchor{i}").transform;
                    child.parent = transform;
                    child.localPosition = new Vector3((i==0?-0.4f:0.4f), 0.55f, 0f);
                    ghostAnchors[i] = child;
                }
            }
        }

        private void RefreshLabel(Room room)
        {
            if (label != null && string.IsNullOrEmpty(label.text)) label.text = "Room";
        }
    }
}
