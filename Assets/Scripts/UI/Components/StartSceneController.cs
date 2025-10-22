using UnityEngine;

public class StartSceneController : MonoBehaviour
{
    [Header("播放完后进入的场景")]
    [SerializeField] private GameScene nextScene = GameScene.MainMenu;

    [Header("加载外观")]
    [SerializeField] private SlideInAnim slideInFx;


    private bool started = false;

    void Update()
    {
        if (started) return;

        // 任意点击/触摸/按键
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.anyKeyDown)
        {
            started = true;
            PlayIntroAndGo();
        }
    }

    private void PlayIntroAndGo()
    {
        if (slideInFx != null)
        {
            slideInFx.OnFinished = () =>
            {
                SceneLoader.Instance.LoadScene(GameScene.MainMenu, true); // 黑屏加载
            };
            slideInFx.Play();
            return;
        }
        
        SceneLoader.Instance.LoadScene(GameScene.MainMenu, true);
    }
}
