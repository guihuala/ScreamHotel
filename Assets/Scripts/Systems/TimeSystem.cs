namespace ScreamHotel.Systems
{
    [System.Serializable]
    public class TimeSystem
    {
        public float dayDurationInSeconds = 300f; // 一天5分钟
        public float currentTimeOfDay = 0.5f; // 中午开始
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
    }
}