using UnityEngine;

public sealed class TimeManager : SingletonPersistent<TimeManager>
{
    public float TimeFactor { get; private set; } = 1f;
    public bool IsPaused { get; private set; }
    private float _baseFixedDeltaTime;
    
    public float DeltaTime => IsPaused ? 0f : Time.unscaledDeltaTime * TimeFactor;

    protected override void Awake()
    {
        base.Awake();

        _baseFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Update()
    {
        // 正确设置全局缩放（暂停/倍速）
        Time.timeScale = IsPaused ? 0f : Mathf.Max(0f, TimeFactor);
        Time.fixedDeltaTime = _baseFixedDeltaTime * Time.timeScale;
    }
    
    public void PauseTime()  { IsPaused = true;  Debug.Log("[TimeManager] paused"); }
    public void ResumeTime() { IsPaused = false; Debug.Log("[TimeManager] resumed"); }
}