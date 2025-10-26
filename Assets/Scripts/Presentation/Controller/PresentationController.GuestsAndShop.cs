using System.Collections.Generic;
using System.Linq;
using ScreamHotel.Core;
using UnityEngine;

namespace ScreamHotel.Presentation
{
    public partial class PresentationController
    {
        private void BuildInitialGuests()
        {
            if (game.State != Core.GameState.NightShow)
            {
                ClearGuestViews();
                return;
            }

            var w = game.World;
            foreach (var g in w.Guests)
            {
                if (_guestViews.ContainsKey(g.Id)) continue;

                int idx = _guestViews.Count;
                var finalPos = GetGuestQueueWorldPos(idx);
                var spawnPos = finalPos - Vector3.right * guestSpawnWalkDistance;

                var gv = Instantiate(guestPrefab, spawnPos, Quaternion.identity, guestsRoot);
                gv.BindGuest(g.Id);
                _guestViews[g.Id] = gv;

                StartCoroutine(SpawnWalkTo(gv.transform, finalPos, guestSpawnWalkDuration));
            }
        }

        private void SyncGuestsQueue()
        {
            // 只在 NightShow 同步；其它阶段确保清空并退出
            if (game.State != GameState.NightShow)
            {
                ClearGuestViews();
                return;
            }

            var w = game.World;

            // 删除不在世界里的旧客人
            var alive = new HashSet<string>(w.Guests.Select(g => g.Id));
            var toRemove = _guestViews.Keys.Where(id => !alive.Contains(id)).ToList();
            foreach (var id in toRemove)
            {
                SafeDestroy(_guestViews[id]?.gameObject);
                _guestViews.Remove(id);
            }

            // 补齐新客人
            for (int i = 0; i < w.Guests.Count; i++)
            {
                var g = w.Guests[i];
                if (_guestViews.ContainsKey(g.Id)) continue;
                
                var finalPos = GetGuestQueueWorldPos(i);
                var spawnPos = finalPos - Vector3.right * guestSpawnWalkDistance;

                var gv = Instantiate(guestPrefab, spawnPos, Quaternion.identity, guestsRoot);
                gv.BindGuest(g.Id);
                _guestViews[g.Id] = gv;

                StartCoroutine(SpawnWalkTo(gv.transform, finalPos, guestSpawnWalkDuration));
            }

            // 全量排队到位
            for (int i = 0; i < w.Guests.Count; i++)
            {
                var id = w.Guests[i].Id;
                if (_guestViews.TryGetValue(id, out var gv))
                {
                    var tmp = new GameObject("tmpTarget").transform;
                    tmp.position = GetGuestQueueWorldPos(i);
                    
                    gv.MoveTo(tmp, 0.2f);
                    SafeDestroy(tmp.gameObject);
                }
            }
        }

        private Vector3 GetGuestQueueWorldPos(int index)
        {
            if (!guestQueueRoot)
            {
                var origin = guestsRoot ? guestsRoot.position : (ghostsRoot ? ghostsRoot.position : Vector3.zero);
                return new Vector3(origin.x + index * queueSpacingX, origin.y, queueFixedZ);
            }

            var box = guestQueueRoot.GetComponentInChildren<BoxCollider>();
            if (box)
            {
                var size = box.size;
                var center = box.center;
                int perRowAuto = Mathf.Max(1, Mathf.FloorToInt(size.x / Mathf.Max(0.01f, queueSpacingX)));
                int perRow = (queueWrapCount > 0) ? Mathf.Min(queueWrapCount, perRowAuto) : perRowAuto;
                int row = index / perRow;
                int col = index % perRow;

                float startX = center.x - (perRow - 1) * 0.5f * queueSpacingX;
                float x = startX + col * queueSpacingX;
                float y = center.y - row * queueRowHeight;

                var local = new Vector3(x, y, queueFixedZ);
                var world = box.transform.TransformPoint(local);
                world.z = queueFixedZ;
                return world;
            }
            else
            {
                int perRow = (queueWrapCount > 0) ? queueWrapCount : 8;
                int row = index / perRow;
                int col = index % perRow;
                float startXLocal = -(perRow - 1) * 0.5f * queueSpacingX;
                var local = new Vector3(startXLocal + col * queueSpacingX, -row * queueRowHeight, queueFixedZ);
                return guestQueueRoot.TransformPoint(local);
            }
        }

        private void ClearGuestViews()
        {
            if (_guestViews.Count == 0) return;
            foreach (var id in _guestViews.Keys.ToList())
            {
                SafeDestroy(_guestViews[id]?.gameObject);
            }

            _guestViews.Clear();
        }

        private System.Collections.IEnumerator Co_BuildGuestsThenSync()
        {
            BuildInitialGuests(); // 会对新客人启动右移协程
            // 等待“右移表演”时间 + 微小缓冲
            yield return new WaitForSeconds(Mathf.Max(0.01f, guestSpawnWalkDuration) + 0.05f);
            SyncGuestsQueue();
        }
        
        private void TrySetSpineAnim(Component root, string anim, bool loop)
        {
            if (string.IsNullOrEmpty(anim) || root == null) return;

            // 尝试 SkeletonAnimation
            var sa = root.GetComponent("Spine.Unity.SkeletonAnimation");
            if (sa != null)
            {
                var state = sa.GetType().GetProperty("AnimationState")?.GetValue(sa, null);
                var setAnim = state?.GetType().GetMethod("SetAnimation",
                    new System.Type[] { typeof(int), typeof(string), typeof(bool) });
                setAnim?.Invoke(state, new object[] { 0, anim, loop });
                return;
            }
        }
        
        private System.Collections.IEnumerator SpawnWalkTo(Transform guest, Vector3 finalPos, float duration)
        {
            if (guest == null || duration <= 0f) yield break;

            // 播放走路动画
            TrySetSpineAnim(guest, spineWalkAnim, true);

            // 用临时目标让 GuestView 自己插值
            var tmp = new GameObject("GuestSpawnWalkTarget").transform;
            tmp.position = finalPos;

            var gv = guest.GetComponent<GuestView>();
            if (gv != null)
            {
                gv.MoveTo(tmp, duration);
            }

            yield return new WaitForSeconds(duration);

            // 切待机
            TrySetSpineAnim(guest, spineIdleAnim, true);
            if (tmp) Destroy(tmp.gameObject);
        }

        // -------- Shop ----------
        private Vector3 GetShopSlotWorldPos(int index)
        {
            if (!shopRoot) return Vector3.zero;
            int perRow = Mathf.Max(1, shopSlotsPerRow);
            int col = index % perRow;
            int row = index / perRow;

            float startX = -(perRow - 1) * 0.5f * shopSlotSpacingX;
            var local = new Vector3(startX + col * shopSlotSpacingX, -row * 0.8f, shopFixedZ);
            return shopRoot.TransformPoint(local);
        }

        private void SyncShop()
        {
            var offers = game.World.Shop.Offers;

            // 1) 删除已下架
            var alive = new HashSet<string>(offers.Select(o => o.OfferId));
            var toRemove = _shopOfferViews.Keys.Where(id => !alive.Contains(id)).ToList();
            foreach (var id in toRemove)
            {
                SafeDestroy(_shopOfferViews[id]?.gameObject);
                _shopOfferViews.Remove(id);
            }

            // 2) 补齐 / 重绑 / 定位
            for (int i = 0; i < offers.Count; i++)
            {
                var off = offers[i];

                if (!_shopOfferViews.TryGetValue(off.OfferId, out var t) || !t)
                {
                    if (!shopSlotPrefab || !shopRoot) continue;
                    var slot = Instantiate(shopSlotPrefab, shopRoot);
                    slot.name = $"Slot_{i+1}_{off.Main}";
                    slot.Rebind(off.Main, i);
                    t = slot.transform;
                    _shopOfferViews[off.OfferId] = t;
                }
                else
                {
                    var slotView = t.GetComponent<ShopSlotView>();
                    if (slotView != null && (slotView.slotIndex != i || slotView.main != off.Main))
                    {
                        slotView.Rebind(off.Main, i);
                    }
                }

                t.position = GetShopSlotWorldPos(i);
            }

            //  3) 同步刷新按钮可见性：有货可见；全卖空隐藏/禁用
            UpdateShopRerollButton();
        }
        
        private void UpdateShopRerollButton()
        {
            if (!shopRerollPrefab) return;
            bool hasAny = game.World.Shop.Offers.Count > 0;
            shopRerollPrefab.SetActive(hasAny);
        }
    }
}
