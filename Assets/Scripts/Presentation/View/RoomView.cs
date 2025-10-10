using UnityEngine;
using ScreamHotel.Domain;
using ScreamHotel.UI;


namespace ScreamHotel.Presentation
{
    public class RoomView : MonoBehaviour, IHoverInfoProvider
    {
        [Header("Binding")]
        public string roomId;
        public MeshRenderer plate;       // 地块渲染器（高亮/着色）
        public Transform[] ghostAnchors; // 鬼锚点

        [Header("Decor Sets (可选)")]
        public GameObject[] lv1Set;
        public GameObject[] lv2Set;
        public GameObject[] lv3Set;
        
        [Header("Guest Anchors (optional)")]
        public Transform[] guestAnchors;

        private Color _baseColor;

        void Awake()
        {
            if (plate != null) 
            {
                // 确保每个实例有独立材质
                _baseColor = plate.material.color;
            }
        }

        public void Bind(Room room)
        {
            roomId = room.Id;
            EnsureAnchors(room.Capacity);
            Refresh(room);

            // 在绑定之后设置 RoomDropZone 的 roomId
            var roomDropZone = GetComponentInParent<RoomDropZone>();
            if (roomDropZone != null)
            {
                roomDropZone.SetRoomId(roomId);  // 设置 RoomDropZone 的 roomId
            }
        }

        public void Refresh(Room room)
        {
            EnsureAnchors(room.Capacity);
            ApplyVisualByLevel(room);
            TintByTag(room);
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

        public Transform GetAnchor(int index) =>
            (ghostAnchors != null && index >= 0 && index < ghostAnchors.Length) ? ghostAnchors[index] : transform;
        
        public bool TryGetGuestAnchor(int index, out Transform anchor)
        {
            anchor = null;
            if (guestAnchors != null && guestAnchors.Length > 0)
            {
                index = Mathf.Clamp(index, 0, guestAnchors.Length - 1);
                // 确保为空也能兜底
                anchor = guestAnchors[index] != null ? guestAnchors[index] : transform;
                return true;
            }
            return false;
        }

        public Transform GetGuestAnchor(int index)
        {
            if (guestAnchors == null || guestAnchors.Length == 0)
            {
                // 默认临时生成两个在另一侧
                guestAnchors = new Transform[2];
                for (int i=0;i<guestAnchors.Length;i++)
                {
                    var t = new GameObject($"GuestAnchor{i}").transform;
                    t.SetParent(transform, false);
                    t.localPosition = new Vector3((i==0?0.6f:0.2f), 0.55f, 0f); // 和鬼的 -0.4/0.4 相反侧
                    guestAnchors[i] = t;
                }
            }
            index = Mathf.Clamp(index, 0, guestAnchors.Length-1);
            return guestAnchors[index];
        }
        
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
        
        public HoverInfo GetHoverInfo() => new HoverInfo
        {
            Kind = HoverKind.Room,
            RoomId = roomId,
            WorldPosition = transform.position
        };
    }
}