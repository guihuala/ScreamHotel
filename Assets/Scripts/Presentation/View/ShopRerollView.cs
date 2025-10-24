using UnityEngine;
using ScreamHotel.UI;
using ScreamHotel.Core;
using DG.Tweening;

namespace ScreamHotel.Presentation.Shop
{
    [RequireComponent(typeof(Collider))]
    public class ShopRerollView : MonoBehaviour, IHoverInfoProvider, IClickActionProvider
    {
        [Header("Animation")]
        public float hoverScaleUp = 1.2f;
        public float hoverDuration = 0.2f;
        public float clickPunchStrengthY = 0.3f;
        public float clickPunchDuration = 0.25f;
        private Tween _hoverTween;
        private Tween _clickTween;

        private Vector3 _originalScale;

        void Awake()
        {
            _originalScale = transform.localScale;
        }

        void OnMouseEnter()
        {
            _hoverTween?.Kill();

            Vector3 targetScale = new Vector3(
                _originalScale.x,
                _originalScale.y * hoverScaleUp,
                _originalScale.z
            );

            _hoverTween = transform
                .DOScale(targetScale, hoverDuration)
                .SetEase(Ease.OutBack);
        }

        void OnMouseExit()
        {
            _hoverTween?.Kill();

            // 回到原始比例
            _hoverTween = transform
                .DOScale(_originalScale, hoverDuration)
                .SetEase(Ease.InBack);
        }

        public HoverInfo GetHoverInfo()
        {
            var game = FindObjectOfType<Game>();
            int cost = game?.World?.Config?.Rules?.ghostShopRerollCost ?? 0;
            return new HoverInfo
            {
                Kind = HoverKind.ShopReroll,
                Cost = cost,
            };
        }

        public bool TryClick(Game game)
        {
            if (game == null) return false;
            bool ok = game.ShopTryReroll();
            if (ok)
            {
                PlayClickEffect();
                var pc = FindObjectOfType<PresentationController>();
                if (pc) pc.SendMessage("SyncShop", SendMessageOptions.DontRequireReceiver);
            }
            return ok;
        }

        private void PlayClickEffect()
        {
            _clickTween?.Kill();
            // 只在Y轴方向上做“punch”
            _clickTween = transform.DOPunchScale(
                new Vector3(0f, clickPunchStrengthY, 0f), 
                clickPunchDuration, 
                vibrato: 1, 
                elasticity: 0.5f
            );
        }
    }
}
