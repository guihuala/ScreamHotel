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

        [Header("Decor Sets (通用)")]
        public GameObject[] lv1Set;

        [Header("Decor Sets for Lv2 by FearTag (可选)")]
        public GameObject[] lv2DarknessSet;
        public GameObject[] lv2BloodSet;
        public GameObject[] lv2NoiseSet;
        public GameObject[] lv2RotSet;
        public GameObject[] lv2GazeSet;

        [Header("Guest Anchors (optional)")]
        public Transform[] guestAnchors;

        [Header("Locked Curtain")]
        public GameObject curtain;

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
                roomDropZone.SetRoomId(roomId);
            }
        }

        public void Refresh(Room room)
        {
            EnsureAnchors(room.Capacity);
            ApplyVisualByLevel(room);
        }

        // ----- 视觉切换 -----
        private void ApplyVisualByLevel(Room room)
        {
            // 幕布：仅 Lv0 显示
            if (curtain) curtain.SetActive(room.Level == 0);

            // 先全关
            SetActiveArray(lv1Set, false);
            SetActiveArray(lv2DarknessSet, false);
            SetActiveArray(lv2BloodSet, false);
            SetActiveArray(lv2NoiseSet, false);
            SetActiveArray(lv2RotSet, false);
            SetActiveArray(lv2GazeSet, false);

            if (room.Level == 1)
            {
                SetActiveArray(lv1Set, true);
            }
            else if (room.Level == 2)
            {
                if (room.RoomTag.HasValue && ActivateLv2ByTag(room.RoomTag.Value))
                {
                    // 已按 Tag 启用对应外观
                }
            }
        }

        /// <summary>
        /// 根据 FearTag 启用对应的 Lv2 外观；若对应组为空，返回 false（外部会回退到 lv2Set）
        /// </summary>
        private bool ActivateLv2ByTag(FearTag tag)
        {
            switch (tag)
            {
                case FearTag.Darkness:
                    if (HasAny(lv2DarknessSet)) { SetActiveArray(lv2DarknessSet, true); return true; }
                    break;
                case FearTag.Blood:
                    if (HasAny(lv2BloodSet)) { SetActiveArray(lv2BloodSet, true); return true; }
                    break;
                case FearTag.Noise:
                    if (HasAny(lv2NoiseSet)) { SetActiveArray(lv2NoiseSet, true); return true; }
                    break;
                case FearTag.Rot:
                    if (HasAny(lv2RotSet)) { SetActiveArray(lv2RotSet, true); return true; }
                    break;
                case FearTag.Gaze:
                    if (HasAny(lv2GazeSet)) { SetActiveArray(lv2GazeSet, true); return true; }
                    break;
            }
            return false;
        }
        
        private static bool HasAny(GameObject[] arr)
        {
            if (arr == null || arr.Length == 0) return false;
            foreach (var go in arr) if (go != null) return true;
            return false;
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
                anchor = guestAnchors[index] != null ? guestAnchors[index] : transform;
                return true;
            }
            return false;
        }

        public Transform GetGuestAnchor(int index)
        {
            if (guestAnchors == null || guestAnchors.Length == 0)
            {
                guestAnchors = new Transform[2];
                for (int i = 0; i < guestAnchors.Length; i++)
                {
                    var t = new GameObject($"GuestAnchor{i}").transform;
                    t.SetParent(transform, false);
                    t.localPosition = new Vector3((i == 0 ? 0.6f : 0.2f), 0.55f, 0f);
                    guestAnchors[i] = t;
                }
            }
            index = Mathf.Clamp(index, 0, guestAnchors.Length - 1);
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
                        child.localPosition = new Vector3((i == 0 ? -0.4f : 0.4f), 0.55f, 0f);
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
        };
    }
}
