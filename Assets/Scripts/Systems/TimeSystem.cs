using ScreamHotel.Core;
using UnityEngine;

namespace ScreamHotel.Systems
{
    public class TimeSystem
    {
        public float currentTimeOfDay = 0f;  // 当前时间（0 到 1 之间）
        public float dayDurationInSeconds;  // 一天的时长
        private float dayStartTime;          // 每日开始时间

        private Game _game;

        public TimeSystem(Game game)
        {
            _game = game;

            // 从 GameRuleConfig 获取设置
            var rules = _game.World?.Config?.Rules;
            if (rules != null)
            {
                dayDurationInSeconds = rules.dayDurationInSeconds;
                dayStartTime = rules.dayStartTime;
            }
        }

        public void Update(float deltaTime)
        {
            if (deltaTime <= 0f) return;  // 被 TimeManager 暂停时直接不推进

            // 正常推进时间（0~1）
            currentTimeOfDay = Mathf.Repeat(currentTimeOfDay + deltaTime / dayDurationInSeconds, 1f);

            // 按照新的配置，计算时间段
            var rules = _game?.World?.Config?.Rules;
            float rDay = rules?.dayRatio ?? 0.50f;
            float rShow = rules?.nightShowRatio ?? 0.20f;
            float rExec = rules?.nightExecuteRatio ?? 0.20f;
            float rSettle = rules?.settlementRatio ?? 0.10f;

            // 设置时间比例
            float dayEnd = rDay;
            float showEnd = dayEnd + rShow;
            float execEnd = showEnd + rExec;

            // 根据时间进度调整游戏状态
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

        public GameState GetCurrentTimePeriod()
        {
            var rules = _game?.World?.Config?.Rules;
            float rDay = rules?.dayRatio ?? 0.50f;
            float rShow = rules?.nightShowRatio ?? 0.20f;
            float rExec = rules?.nightExecuteRatio ?? 0.20f;

            float currentTime = currentTimeOfDay;  // 获取当前时间进度

            if (currentTime >= 0f && currentTime < rDay) return GameState.Day;  // 白天
            if (currentTime >= rDay && currentTime < rDay + rShow) return GameState.NightShow;  // 夜间展示
            if (currentTime >= rDay + rShow && currentTime < rDay + rShow + rExec) return GameState.NightExecute; // 夜间执行
            return GameState.Settlement;  // 结算
        }
    }
}
