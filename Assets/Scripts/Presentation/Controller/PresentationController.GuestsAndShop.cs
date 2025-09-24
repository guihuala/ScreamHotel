using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScreamHotel.Presentation
{
    public partial class PresentationController
    {
        private void BuildInitialGuests()
        {
            var w = game.World;
            foreach (var g in w.Guests)
            {
                if (_guestViews.ContainsKey(g.Id)) continue;
                var gv = Instantiate(guestPrefab, guestsRoot);
                gv.BindGuest(g.Id);
                gv.SnapTo(GetGuestQueueWorldPos(_guestViews.Count)); // 生成即站队
                _guestViews[g.Id] = gv;
            }
        }

        private void SyncGuestsQueue()
        {
            var w = game.World;

            // 删除不在世界里的旧客人
            var alive = new HashSet<string>(w.Guests.Select(g => g.Id));
            var toRemove = _guestViews.Keys.Where(id => !alive.Contains(id)).ToList();
            foreach (var id in toRemove) { SafeDestroy(_guestViews[id]?.gameObject); _guestViews.Remove(id); }

            // 补齐新客
            foreach (var g in w.Guests)
            {
                if (_guestViews.ContainsKey(g.Id)) continue;
                var gv = Instantiate(guestPrefab, guestsRoot);
                gv.BindGuest(g.Id);
                _guestViews[g.Id] = gv;
            }

            // 全量排队到位
            for (int i = 0; i < w.Guests.Count; i++)
            {
                var id = w.Guests[i].Id;
                if (_guestViews.TryGetValue(id, out var gv))
                {
                    var tmp = new GameObject("tmpTarget").transform; tmp.position = GetGuestQueueWorldPos(i);
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
                var size = box.size; var center = box.center;
                int perRowAuto = Mathf.Max(1, Mathf.FloorToInt(size.x / Mathf.Max(0.01f, queueSpacingX)));
                int perRow = (queueWrapCount > 0) ? Mathf.Min(queueWrapCount, perRowAuto) : perRowAuto;
                int row = index / perRow; int col = index % perRow;

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
                int row = index / perRow; int col = index % perRow;
                float startXLocal = -(perRow - 1) * 0.5f * queueSpacingX;
                var local = new Vector3(startXLocal + col * queueSpacingX, -row * queueRowHeight, queueFixedZ);
                return guestQueueRoot.TransformPoint(local);
            }
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

            // 删除下架
            var alive = new HashSet<string>(offers.Select(o => o.OfferId));
            var toRemove = _shopOfferViews.Keys.Where(id => !alive.Contains(id)).ToList();
            foreach (var id in toRemove) { SafeDestroy(_shopOfferViews[id]?.gameObject); _shopOfferViews.Remove(id); }

            // 补齐/定位
            for (int i = 0; i < offers.Count; i++)
            {
                var off = offers[i];
                if (!_shopOfferViews.TryGetValue(off.OfferId, out var t) || !t)
                {
                    if (!shopOfferPrefab) continue;
                    t = Instantiate(shopOfferPrefab, shopRoot);
                    t.name = $"Offer_{i+1}_{off.Main}";
                    _shopOfferViews[off.OfferId] = t;
                }
                t.position = GetShopSlotWorldPos(i);
            }
        }
    }
}
