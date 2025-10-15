using ScreamHotel.Core;
using UnityEngine;
using UnityEngine.UI;

public class ComicPlayer : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;  // 播放动态漫画的 Animator / Timeline 驱动器
    public Button skipButton;  // 跳过按钮（可选）

    private void Start()
    {
        // 根据成功/失败，播放不同动画状态
        bool success = EndingContext.Result.Success;
        if (animator != null)
        {
            string state = success ? "Comic_Success" : "Comic_Fail";
            animator.Play(state, 0, 0f);
        }

        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkip);
    }

    // 漫画播完（或点击跳过）后回主菜单或重新开局
    public void OnComicFinished() => SceneLoader.Instance.LoadScene(GameScene.MainMenu);
    private void OnSkip() => OnComicFinished();
}