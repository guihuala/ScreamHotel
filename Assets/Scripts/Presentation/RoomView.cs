using UnityEngine;
using ScreamHotel.Domain;


namespace ScreamHotel.Presentation
{
    public class RoomView : MonoBehaviour
    {
        [Header("Binding")]
        public string roomId;
        public MeshRenderer plate;       // 地块渲染器（高亮/着色）
        public Transform[] ghostAnchors; // 鬼锚点

        [Header("Decor Sets (可选)")]
        public GameObject[] lv1Set;
        public GameObject[] lv2Set;
        public GameObject[] lv3Set;

        [Header("UI (Optional)")]
        public TextMesh label;

        private Color _baseColor;
        private float _pulseT; private Color _targetColor;

        void Awake()
        {
            if (plate != null) _baseColor = plate.sharedMaterial.color;
            if (label && string.IsNullOrEmpty(label.text)) label.text = "Room";
        }

        public void Bind(Room room)
        {
            roomId = room.Id;
            EnsureAnchors(room.Capacity);
            Refresh(room);
        }

        public void Refresh(Room room)
        {
            // 文本
            var tag = room.RoomTag.HasValue ? room.RoomTag.Value.ToString() : "-";
            if (label) label.text = $"{room.Id}  Lv{room.Level}  [{tag}]  Cap:{room.Capacity}";

            EnsureAnchors(room.Capacity);
            ApplyVisualByLevel(room);   // ★ 根据等级切外观
            TintByTag(room);            // ★ 根据 Tag 微调底板色
        }

        // ----- 视觉切换 -----
        private void ApplyVisualByLevel(Room room)
        {
            SetActiveArray(lv1Set, room.Level == 1);
            SetActiveArray(lv2Set, room.Level == 2);
            SetActiveArray(lv3Set, room.Level == 3);
        }

        private void TintByTag(Room room)
        {
            if (plate == null) return;
            // Lv2/Lv3 才有房间属性装饰，这里做一点点颜色偏移作为占位
            if (room.Level >= 2 && room.RoomTag.HasValue)
            {
                var c = TagColor(room.RoomTag.Value);
                var mixed = Color.Lerp(_baseColor, c, 0.25f);
                plate.material.color = mixed;
            }
            else
            {
                plate.material.color = _baseColor;
            }
        }

        private Color TagColor(FearTag tag)
        {
            switch (tag)
            {
                case FearTag.Darkness: return new Color(0.35f,0.35f,1f);
                case FearTag.Blood:    return new Color(1f,0.3f,0.3f);
                case FearTag.Noise:    return new Color(1f,0.8f,0.2f);
                case FearTag.Rot:      return new Color(0.55f,0.8f,0.3f);
                case FearTag.Gaze:     return new Color(0.8f,0.5f,1f);
                default: return _baseColor;
            }
        }

        private void SetActiveArray(GameObject[] arr, bool on)
        {
            if (arr == null) return;
            foreach (var go in arr) if (go) go.SetActive(on);
        }

        // ----- 脉冲反馈（保留） -----
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
                plate.material.color = Color.Lerp(plate.material.color, _targetColor, Mathf.SmoothStep(0,1,t));
                if (_pulseT <= 0f) TintByTag(new Room{ Level = 1, RoomTag = null }); // 回到基础/Tag色
            }
        }

        public Transform GetAnchor(int index) =>
            (ghostAnchors != null && index >= 0 && index < ghostAnchors.Length) ? ghostAnchors[index] : transform;

        private void EnsureAnchors(int capacity)
        {
            if (ghostAnchors == null || ghostAnchors.Length < capacity)
            {
                int old = ghostAnchors != null ? ghostAnchors.Length : 0;
                var newArr = new Transform[Mathf.Max(capacity, 1)];
                for (int i = 0; i < newArr.Length; i++)
                {
                    if (ghostAnchors != null && i < old && ghostAnchors[i] != null)
                        newArr[i] = ghostAnchors[i];
                    else
                    {
                        var child = transform.Find($"Anchor{i}") ?? new GameObject($"Anchor{i}").transform;
                        child.parent = transform;
                        child.localPosition = new Vector3((i==0?-0.4f:0.4f), 0.55f, 0f);
                        newArr[i] = child;
                    }
                }
                ghostAnchors = newArr;
            }
        }
    }
}