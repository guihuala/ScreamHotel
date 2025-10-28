using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace ScreamHotel.Presentation
{
    public class SoundManager : MonoBehaviour
    {
        [Header("游戏BGM播放器")] 
        public AudioSource _musicSource;
        
        private const string MusicPathPrefix = "Audio/BGM/"; // 音乐路径前缀
        private bool isFading = false; // 当前是否正在淡出

        public void PlayMusic(string musicName, float fadeDuration = 0.5f)
        {
            if (_musicSource.isPlaying && !isFading)
            {
                StartCoroutine(FadeOutCurrentMusic(fadeDuration, musicName, fadeDuration));
            }
            else
            {
                StartCoroutine(FadeInMusic(musicName, fadeDuration));
            }
        }

        private IEnumerator FadeInMusic(string musicName, float fadeDuration)
        {
            string path = MusicPathPrefix + musicName;
            AudioClip newClip = Resources.Load<AudioClip>(path);

            if (newClip != null)
            {
                _musicSource.clip = newClip;
                _musicSource.Play();

                _musicSource.volume = 0f;
                _musicSource.DOFade(1f, fadeDuration).SetUpdate(true);
            }
            else
            {
                Debug.LogError($"未找到音乐文件：{path}");
            }

            yield return null;
        }

        private IEnumerator FadeOutCurrentMusic(float fadeDuration, string nextMusic, float fadeInDuration)
        {
            isFading = true;
            _musicSource.DOFade(0f, fadeDuration).SetUpdate(true);

            yield return new WaitForSeconds(fadeDuration);

            _musicSource.Stop(); 
            StartCoroutine(FadeInMusic(nextMusic, fadeInDuration));
            
            isFading = false;
        }
    }
}
