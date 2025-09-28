using UnityEngine;

namespace ScreamHotel.Systems
{
    [System.Serializable]
    public class TimeSystem
    {
        public float dayDurationInSeconds = 30f; // 一天5分钟
        public float currentTimeOfDay = 0.25f; // 从早晨开始（6:00）
        public bool isPaused = false;

        public float DayProgress => currentTimeOfDay;
        public bool IsNight => currentTimeOfDay > 0.75f || currentTimeOfDay < 0.25f;

        public void Update(float deltaTime)
        {
            if (!isPaused)
            {
                currentTimeOfDay = (currentTimeOfDay + deltaTime / dayDurationInSeconds) % 1f;
            }
        }
        
        public void SetNormalizedTime(float t)
        {
            currentTimeOfDay = Mathf.Repeat(t, 1f);
        }
    }
}