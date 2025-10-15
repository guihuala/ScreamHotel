using ScreamHotel.Core;
using UnityEngine;

namespace ScreamHotel.Core
{
    // 捕获Game抛的GameEnded
    public class EndingRouter : MonoBehaviour
    {
        private void OnEnable()
        {
            EventBus.Subscribe<GameEnded>(OnGameEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEnded>(OnGameEnded);
        }

        private void OnGameEnded(GameEnded e)
        {
            // 统一切到漫画结局场景
            SceneLoader.Instance.LoadScene(GameScene.EndingComic, () =>
            {
                // 可在加载完成后，透传结果到新的场景（如用一个静态数据或单例）
                EndingContext.Result = e;
            });
        }
    }

    // 轻量上下文，把结局信息带到漫画场景
    public static class EndingContext
    {
        public static GameEnded Result;
    }
}