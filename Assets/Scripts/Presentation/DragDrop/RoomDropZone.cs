using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ScreamHotel.Domain;
using ScreamHotel.Systems;
using ScreamHotel.UI;

namespace ScreamHotel.Presentation
{
    [RequireComponent(typeof(Collider))]
    public class RoomDropZone : MonoBehaviour, IDropZone
    {
        [Header("Binding")]
        public MeshRenderer plate;
        public Color canColor = new Color(0.3f, 1f, 0.3f, 1f);
        public Color fullColor = new Color(1f, 0.3f, 0.3f, 1f);

        [Header("Build VFX")]
        [Tooltip("建造/升级时播放的粒子特效预制体")]
        public ParticleSystem buildFxPrefab;

        private Core.Game game;
        private RoomView _rv; // 只用于拿锚点/可视
        private string roomId;
        private Color _origColor;

        // 系统
        private AssignmentSystem _assign => GetSystem<AssignmentSystem>(game, "_assignmentSystem");
        private BuildSystem _build    => GetSystem<BuildSystem>(game, "_buildSystem");
        private object _dayNightSysRaw=> GetSystem<object>(game, "_dayNightSystem");
        private HoverUIController _hoverUI;

        // 正在建造中的鬼，避免重复触发
        private readonly HashSet<string> _busyGhosts = new HashSet<string>();

        void Awake()
        {
            if (plate != null) _origColor = plate.sharedMaterial.color;
            if (game == null) game = FindObjectOfType<Core.Game>();
            _rv = GetComponent<RoomView>();
            _hoverUI = FindObjectOfType<HoverUIController>(true);
        }

        public void SetRoomId(string newRoomId) => roomId = newRoomId;

        // 夜间/非建造分支使用；白天建造逻辑不走容量校验
        public bool CanAccept(string id, bool isGhost = true)
        {
            var w = game.World;
            var r = w.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null) return false;

            if (isGhost)
            {
                var g = w.Ghosts.FirstOrDefault(x => x.Id == id);
                if (g == null || g.State is GhostState.Training) return false;
                if (r.AssignedGhostIds.Count >= r.Capacity) return false;
            }
            else
            {
                var guest = w.Guests.FirstOrDefault(x => x.Id == id);
                if (guest == null) return false;
                if (r.AssignedGuestIds.Count >= r.Capacity) return false;
            }
            return true;
        }

        public bool TryDrop(string id, bool isGhost, out Transform targetAnchor)
        {
            targetAnchor = null;

            var w = game.World;
            var r = w.Rooms.FirstOrDefault(x => x.Id == roomId);
            if (r == null)
            {
                Debug.LogWarning($"房间 {roomId} 未找到！");
                return false;
            }

            // ============ 白天 + 鬼：进入房间进行建造/升级（1~2秒） ============
            if (isGhost && IsDaytime())
            {
                if (_build == null)
                {
                    Debug.LogWarning("BuildSystem 未找到，无法建造/升级。");
                    Flash(fullColor);
                    return false;
                }
                if (_busyGhosts.Contains(id))
                {
                    Debug.Log($"鬼 {id} 正在建造中，忽略重复拖放。");
                    Flash(fullColor);
                    return false;
                }

                // 选择一个锚点让鬼“待在房间里”进行施工（不做数据上的分配）
                var anchor = (_rv != null && _rv.ghostAnchors != null && _rv.ghostAnchors.Length > 0)
                    ? _rv.ghostAnchors[0] : transform;
                targetAnchor = anchor;

                if (r.Level == 0)
                {
                    // Lv0 -> Lv1：无需选择恐惧属性
                    StartCoroutine(Co_BuildOrUpgrade(id, anchor, () =>
                    {
                        // 施工结束时再真正扣费/解锁
                        return _build.TryUnlockRoom(roomId);
                    }));
                    Flash(canColor);
                    return true; // 让鬼进入锚点“施工”
                }
                else if (r.Level == 1)
                {
                    // Lv1 -> Lv2：先选择恐惧属性，然后开始施工
                    if (_hoverUI == null)
                    {
                        Flash(fullColor);
                        return false;
                    }

                    var attach = _rv != null ? _rv.transform : transform;
                    _hoverUI.OpenPickFearPanel(id, attach, 0, (ghostId, tag, slotIdx) =>
                    {
                        // 选完后再让鬼开始“施工 1~2s”，结束时真正升级
                        StartCoroutine(Co_BuildOrUpgrade(ghostId, anchor, () =>
                        {
                            return _build.TryUpgradeRoom(roomId, tag); // Lv1->Lv2 必须有 tag
                        }));
                    });

                    Flash(canColor);
                    return true; // 立刻把鬼放到锚点（等待选择完再开工）
                }
                else if (r.Level == 2)
                {
                    // Lv2 -> Lv3：直接施工，不更新“按属性”的外观
                    StartCoroutine(Co_BuildOrUpgrade(id, anchor, () =>
                    {
                        return _build.TryUpgradeRoom(roomId); // 不改变 tag
                    }));
                    Flash(canColor);
                    return true;
                }
                else
                {
                    Debug.Log("房间已满级，无法继续升级。");
                    Flash(fullColor);
                    return false;
                }
            }

            // ============ 夜晚或非鬼：恢复“分配”逻辑 ============
            if (!CanAccept(id, isGhost))
            {
                Flash(fullColor);
                return false;
            }

            if (isGhost)
            {
                if (_assign != null && _assign.TryAssignGhostToRoom(id, roomId))
                {
                    var index = r.AssignedGhostIds.IndexOf(id);
                    targetAnchor = _rv != null ? _rv.GetAnchor(index) : transform;
                    Flash(canColor);
                    return true;
                }
            }
            else
            {
                if (_assign != null && _assign.TryAssignGuestToRoom(id, roomId))
                {
                    var idx = Mathf.Max(0, r.AssignedGuestIds.IndexOf(id));
                    targetAnchor = _rv != null ? _rv.GetGuestAnchor(idx) : transform;
                    Flash(canColor);
                    return true;
                }
            }

            Flash(fullColor);
            return false;
        }

        /// <summary>
        /// 让鬼在锚点“施工”1秒，期间可播放粒子；结束后调用buildAction（扣费/升级）
        /// 不改变分配状态，结束后鬼仍可被自由拖动
        /// </summary>
        private IEnumerator Co_BuildOrUpgrade(string ghostId, Transform anchor, System.Func<bool> buildAction)
        {
            _busyGhosts.Add(ghostId);

            // 播放粒子
            ParticleSystem fx = null;
            if (buildFxPrefab != null)
            {
                fx = Instantiate(buildFxPrefab, anchor.position, Quaternion.identity);
                fx.Play();
            }
            
            float duration = Random.Range(1f, 1.5f);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                yield return null;
            }

            bool ok = false;
            try
            {
                ok = buildAction != null && buildAction.Invoke();
            }
            finally
            {
                // 停止并销毁粒子
                if (fx != null)
                {
                    fx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    Destroy(fx.gameObject, 0.05f);
                }
                _busyGhosts.Remove(ghostId);
            }

            Flash(ok ? canColor : fullColor);
            if (!ok) Debug.LogWarning("建造/升级失败：可能是金币不足或规则限制。");
        }

        public void ShowHoverFeedbackGuest()
        {
            if (plate == null) return;
            plate.material.color = canColor;
        }

        public void ShowHoverFeedback(string ghostId)
        {
            if (plate == null) return;
            // 白天拖鬼用于建造：容量不影响，直接绿色提示；否则按分配校验
            var c = IsDaytime() ? canColor : (CanAccept(ghostId, true) ? canColor : fullColor);
            plate.material.color = Color.Lerp(plate.material.color, c, 0.5f);
        }

        public void ShowHoverFeedback(string id, bool isGhost)
        {
            if (isGhost) ShowHoverFeedback(id);
            else ShowHoverFeedbackGuest();
        }

        public void ClearFeedback()
        {
            if (plate == null) return;
            plate.material.color = _origColor;
        }

        private static T GetSystem<T>(object obj, string field) where T : class
        {
            var f = obj.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f?.GetValue(obj) as T;
        }

        private void Flash(Color c)
        {
            if (plate == null) return;
            plate.material.color = c;
            CancelInvoke(nameof(Revert));
            Invoke(nameof(Revert), 0.25f);
        }

        private void Revert()
        {
            if (plate != null) plate.material.color = _origColor;
        }

        // 日夜判断；拿不到系统就默认白天
        private bool IsDaytime()
        {
            if (_dayNightSysRaw == null) return true;
            var t = _dayNightSysRaw.GetType();
            var prop = t.GetProperty("IsDay") ?? t.GetProperty("IsDayTime");
            if (prop != null && prop.PropertyType == typeof(bool))
                return (bool)prop.GetValue(_dayNightSysRaw);
            var field = t.GetField("IsDay") ?? t.GetField("IsDayTime");
            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(_dayNightSysRaw);
            return true;
        }
    }
}
