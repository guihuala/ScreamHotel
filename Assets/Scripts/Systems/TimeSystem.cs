using ScreamHotel.Core;
using UnityEngine;

namespace ScreamHotel.Systems
{
    [System.Serializable]
    public class TimeSystem
    {
        public float dayDurationInSeconds = 300f; // 一天5分钟
        public float currentTimeOfDay = 0.25f; // 从早晨开始（6:00）
        public bool isPaused = false;

        private Game _game;

        public TimeSystem(Game game)  // 构造函数接受 Game 实例
        {
            _game = game;
        }

        public float DayProgress => currentTimeOfDay;
        public bool IsNight => currentTimeOfDay > 0.75f || currentTimeOfDay < 0.25f;

        public void Update(float deltaTime)
        {
            if (!isPaused)
            {
                currentTimeOfDay = (currentTimeOfDay + deltaTime / dayDurationInSeconds) % 1f;

                // 根据当前时间比例判断当前阶段
                if (currentTimeOfDay >= 0.50f && currentTimeOfDay < 0.70f)
                {
                    // It's NightShow
                    if (_game.State != GameState.NightShow)
                    {
                        _game.StartNightShow();
                    }
                }
                else if (currentTimeOfDay >= 0.70f && currentTimeOfDay < 0.90f)
                {
                    // It's NightExecute
                    if (_game.State != GameState.NightExecute)
                    {
                        _game.StartNightExecute();
                    }
                }
                else if (currentTimeOfDay >= 0.90f && currentTimeOfDay < 1.00f)
                {
                    // It's Settlement
                    if (_game.State != GameState.Settlement)
                    {
                        _game.StartSettlement();
                    }
                }
                else
                {
                    // It's Day
                    if (_game.State != GameState.Day)
                    {
                        _game.GoToDay();
                    }
                }
            }
        }
        
        public void SetNormalizedTime(float t)
        {
            currentTimeOfDay = Mathf.Repeat(t, 1f);
        }
    }
}
