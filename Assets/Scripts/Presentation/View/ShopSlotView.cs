using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Domain;
using ScreamHotel.UI;
using DG.Tweening;

namespace ScreamHotel.Presentation
{
    public class ShopSlotView : MonoBehaviour, IHoverInfoProvider, IClickActionProvider
    {
        [Header("Bind")]
        public int slotIndex = -1;
        public FearTag main;

        [Header("Visual Roots")]
        public Transform visualRoot;

        [Header("Prefabs")]
        public PawnView pawnPrefab;

        [Header("Layout")]
        public Vector3 pawnLocalOffset = new Vector3(0, 0.5f, 0);
        public float pawnLocalScale = 1.0f;

        [Header("Animation")]
        public float hoverScaleUp = 1.15f;
        public float hoverDuration = 0.2f;
        public float clickFlashDuration = 0.25f;
        public Color clickFlashColor = new Color(1f, 0.9f, 0.5f, 1f);
        private Tween _hoverTween;
        private Tween _flashTween;
        private Renderer _renderer;
        private Color _originalColor;

        void Awake()
        {
            if (Application.isPlaying) BuildVisual();
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null) _originalColor = _renderer.material.color;
        }

        void OnMouseEnter()
        {
            if (!visualRoot) return;
            _hoverTween?.Kill();
            _hoverTween = visualRoot.DOScale(Vector3.one * hoverScaleUp, hoverDuration).SetEase(Ease.OutBack);
        }

        void OnMouseExit()
        {
            if (!visualRoot) return;
            _hoverTween?.Kill();
            _hoverTween = visualRoot.DOScale(Vector3.one, hoverDuration).SetEase(Ease.InBack);
        }

        public HoverInfo GetHoverInfo()
        {
            var game = FindObjectOfType<Game>();
            int price = game?.World?.Config?.Rules?.ghostShopPrice ?? 0;
            return new HoverInfo
            {
                Kind = HoverKind.ShopSlot,
                ShopMain = main,
                ShopPrice = price,
            };
        }

        public bool TryClick(Game game)
        {
            if (game == null) return false;
            bool ok = game.ShopTryBuy(slotIndex, out var newGhostId);
            if (ok)
            {
                PlayClickEffect();

                if (visualRoot)
                {
                    for (int i = visualRoot.childCount - 1; i >= 0; i--)
                        Destroy(visualRoot.GetChild(i).gameObject);
                }

                var pc = FindObjectOfType<PresentationController>();
                if (pc)
                {
                    pc.SendMessage("SyncShop", SendMessageOptions.DontRequireReceiver);
                    pc.SendMessage("SyncGhosts", SendMessageOptions.DontRequireReceiver);
                }
            }
            return ok;
        }

        private void PlayClickEffect()
        {
            if (_renderer == null) return;

            _flashTween?.Kill();
            _flashTween = DOTween.Sequence()
                .Append(_renderer.material.DOColor(clickFlashColor, clickFlashDuration * 0.5f))
                .Append(_renderer.material.DOColor(_originalColor, clickFlashDuration * 0.5f))
                .Play();
        }

        public void Rebind(FearTag newMain, int newIndex)
        {
            main = newMain;
            slotIndex = newIndex;
            BuildVisual();
        }

        private void BuildVisual()
        {
            if (!visualRoot) return;

            for (int i = visualRoot.childCount - 1; i >= 0; i--)
                Destroy(visualRoot.GetChild(i).gameObject);

            if (pawnPrefab)
            {
                var game = FindObjectOfType<Game>();
                var offers = game?.World?.Shop?.Offers;
                GhostOffer offer = null;

                if (offers != null && slotIndex >= 0 && slotIndex < offers.Count)
                    offer = offers[slotIndex];

                var pawn = Instantiate(pawnPrefab, visualRoot);
                pawn.transform.localPosition = pawnLocalOffset;
                pawn.transform.localRotation = Quaternion.identity;
                pawn.transform.localScale = Vector3.one * Mathf.Max(0.01f, pawnLocalScale);

                if (offer != null && !string.IsNullOrEmpty(offer.OfferId))
                {
                    var fake = new Ghost { Id = offer.OfferId, Main = offer.Main };
                    pawn.BindGhost(fake);
                }
                else
                {
                    var fallbackId = $"preview@offer_{slotIndex}_{main}";
                    var fake = new Ghost { Id = fallbackId, Main = main };
                    pawn.BindGhost(fake);
                }
            }
        }
    }
}