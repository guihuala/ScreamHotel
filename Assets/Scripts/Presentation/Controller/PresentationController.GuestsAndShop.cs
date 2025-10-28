using System.Collections.Generic;
using System.Linq;
using ScreamHotel.Core;
using UnityEngine;

namespace ScreamHotel.Presentation
{
    public partial class PresentationController
    {
        private void SyncGuestsQueue()
        {
            if (game.State != GameState.NightShow)
            {
                ClearGuestViews();
                return;
            }

            var w = game.World;

            // 1) 删除不在世界里的旧客人
            var alive = new HashSet<string>(w.Guests.Select(g => g.Id));
            var toRemove = _guestViews.Keys.Where(id => !alive.Contains(id)).ToList();
            foreach (var id in toRemove)
            {
                SafeDestroy(_guestViews[id]?.gameObject);
                _guestViews.Remove(id);
            }

            // 2) 补齐新客人（左侧出生 -> QueueAutoWalker 自动走到位）
            for (int i = 0; i < w.Guests.Count; i++)
            {
                var g = w.Guests[i];
                if (_guestViews.ContainsKey(g.Id)) continue;

                var finalPos = GetGuestQueueWorldPos(i);
                var spawnPos = finalPos - Vector3.right * guestSpawnWalkDistance; // 左侧出生

                var gv = Instantiate(guestPrefab, spawnPos, Quaternion.identity, guestsRoot);
                gv.BindGuest(g.Id);
                _guestViews[g.Id] = gv;

                var qw = gv.GetComponent<QueueAutoWalker>() ?? gv.gameObject.AddComponent<QueueAutoWalker>();
                float speed = Mathf.Max(0.01f, guestSpawnWalkDistance / Mathf.Max(0.01f, guestSpawnWalkDuration));
                qw.Init(finalPos.x, speed);
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
            yield return null;
            SyncGuestsQueue();
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
