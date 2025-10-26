using UnityEngine;

[DefaultExecutionOrder(-10000)] // 很早执行，确保比场景物体更早 Awake
public sealed class TimeManager : SingletonPersistent<TimeManager>
{
    public float TimeFactor { get; private set; } = 1f;
    public bool IsPaused { get; private set; }
    private float _baseFixedDeltaTime;

    // 供“局内时间”使用的 dt（受暂停/倍速控制）
    public float DeltaTime => IsPaused ? 0f : Time.unscaledDeltaTime * TimeFactor;

    // —— 在任何场景加载前就自举一个 TimeManager —— //
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        var go = new GameObject("TimeManager");
        go.hideFlags = HideFlags.DontSave;
        go.AddComponent<TimeManager>();
        DontDestroyOnLoad(go);
    }

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