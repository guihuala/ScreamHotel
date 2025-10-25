using System;
using UnityEngine;
using UnityEngine.UI;

public class TitleUIController : MonoBehaviour
{
    public Button startButton;
    public Button settingsButton;
    public Button creditsButton;
    public Button exitButton;

    [Header("Timeline 漫画播放器")]
    [SerializeField] private TimelineComicPlayer comicPlayer;

    [Header("进入游戏时是否使用黑屏加载")]
    [SerializeField] private bool useBlackoutLoading = true;

    bool _starting = false;

    private void Awake()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        creditsButton.onClick.AddListener(OnCreditsButtonClicked);
        exitButton.onClick.AddListener(OnExitButtonClicked);
    }

    public void OnStartButtonClicked()
    {
        if (_starting) return;
        _starting = true;
        startButton.interactable = false;

        Action go = () =>
        {
            SceneLoader.Instance.LoadScene(GameScene.Game);
        };

        if (comicPlayer != null && !comicPlayer.IsPlaying)
        {
            comicPlayer.PlayComic(go);
        }
    }

    public void OnSettingsButtonClicked()
    {
        UIManager.Instance.OpenPanel("SettingPanel");
    }

    public void OnCreditsButtonClicked()
    {
        UIManager.Instance.OpenPanel("CreditPanel");
    }

    public void OnExitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}