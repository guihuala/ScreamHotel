using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ScreamHotel.Core;
using TMPro;

namespace ScreamHotel.UI
{
    public class GamePhaseNotifier : MonoBehaviour
    {
        [Header("UI Elements")]
        public CanvasGroup rootGroup;
        public TextMeshProUGUI phaseText;

        [Header("Animation")]
        public float fadeInDuration = 0.5f;
        public float holdDuration = 1.8f;
        public float fadeOutDuration = 0.5f;

        private Tween _tween;

        void Awake()
        {
            EventBus.Subscribe<GameStateChanged>(OnGameStateChanged);
            if (rootGroup)
            {
                rootGroup.alpha = 0f;
                rootGroup.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            EventBus.Unsubscribe<GameStateChanged>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChanged evt)
        {
            if (evt.State is not GameState state) return;
            
            string msg = state switch
            {
                GameState.Day => "A new day begins",
                GameState.NightShow => "Night falls",
                GameState.NightExecute => "Night execution in progress",
                GameState.Settlement => "Settlement phase",
                _ => ""
            };
            
            ShowMessage(msg);
        }

        private void ShowMessage(string message)
        {
            if (rootGroup == null || phaseText == null) return;

            _tween?.Kill();
            phaseText.text = message;
            rootGroup.gameObject.SetActive(true);
            rootGroup.alpha = 0f;

            // 淡入 → 停留 → 淡出
            _tween = DOTween.Sequence()
                .Append(rootGroup.DOFade(1f, fadeInDuration))
                .AppendInterval(holdDuration)
                .Append(rootGroup.DOFade(0f, fadeOutDuration))
                .OnComplete(() => rootGroup.gameObject.SetActive(false));
        }
    }
}
