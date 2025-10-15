using ScreamHotel.Core;
using UnityEngine;

namespace ScreamHotel.Systems
{
    [System.Serializable]
    public class TimeSystem
    {
        public float dayDurationInSeconds = 300f; // 一天5分钟
        public float currentTimeOfDay = 0.25f;    // 从早晨开始（6:00）

        // 把 isPaused 变为转发到 TimeManager
        public bool isPaused
        {
            get => TimeManager.Instance != null && TimeManager.Instance.IsPaused;
            set
            {
                if (TimeManager.Instance == null) return;
                if (value) TimeManager.Instance.PauseTime();
                else TimeManager.Instance.ResumeTime();
            }
        }

        private Game _game;

        public TimeSystem(Game game)  { _game = game; }

        public float DayProgress => currentTimeOfDay;
        public bool IsNight => currentTimeOfDay > 0.75f || currentTimeOfDay < 0.25f;

        public void Update(float deltaTime)
        {
            if (deltaTime <= 0f) return; // 被 TimeManager 暂停时直接不推进

            // 正常推进时间（0~1）
            currentTimeOfDay = (currentTimeOfDay + deltaTime / dayDurationInSeconds) % 1f;

            var rules = _game?.World?.Config?.Rules;
            float rDay = 0.50f, rShow = 0.20f, rExec = 0.20f, rSettle = 0.10f;

            if (rules != null)
            {
                rDay   = Mathf.Max(0f, rules.dayRatio);
                rShow  = Mathf.Max(0f, rules.nightShowRatio);
                rExec  = Mathf.Max(0f, rules.nightExecuteRatio);
                rSettle= Mathf.Max(0f, rules.settlementRatio);
                var sum = rDay + rShow + rExec + rSettle;
                if (sum > 0.0001f)
                {
                    rDay   /= sum; rShow /= sum; rExec /= sum; rSettle /= sum;
                }
                else { rDay=0.50f; rShow=0.20f; rExec=0.20f; rSettle=0.10f; }
            }

            float dayEnd   = rDay;
            float showEnd  = dayEnd + rShow;
            float execEnd  = showEnd + rExec;

            if (currentTimeOfDay >= dayEnd && currentTimeOfDay < showEnd)
            {
                if (_game.State != GameState.NightShow) _game.StartNightShow();
            }
            else if (currentTimeOfDay >= showEnd && currentTimeOfDay < execEnd)
            {
                if (_game.State != GameState.NightExecute) _game.StartNightExecute();
            }
            else if (currentTimeOfDay >= execEnd && currentTimeOfDay < 1.00f)
            {
                if (_game.State != GameState.Settlement) _game.StartSettlement();
            }
            else
            {
                if (_game.State != GameState.Day) _game.GoToDay();
            }
        }

        public void SetNormalizedTime(float t)
        {
            currentTimeOfDay = Mathf.Repeat(t, 1f);
        }
    }
}

