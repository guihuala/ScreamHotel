using UnityEngine;
using System.Collections.Generic;

public enum CursorState
{
    Default,
    Clickable,  // 可点击状态
    Forbidden,  // 禁止状态
}

public class CursorManager : SingletonPersistent<CursorManager>
{
    [System.Serializable]
    public class CursorStateData
    {
        public CursorState state;
        public Texture2D texture;
        public Vector2 hotspot = Vector2.zero;
        public CursorMode mode = CursorMode.Auto;
    }

    public List<CursorStateData> cursorStates = new List<CursorStateData>();
    
    private CursorState currentState = CursorState.Default;
    private Dictionary<CursorState, CursorStateData> stateDictionary = new Dictionary<CursorState, CursorStateData>();

    protected override void Awake()
    {
        base.Awake();
        InitializeCursorStates();
        SetCursorState(CursorState.Default);
    }

    private void InitializeCursorStates()
    {
        stateDictionary.Clear();
        foreach (var stateData in cursorStates)
        {
            stateDictionary[stateData.state] = stateData;
        }
    }

    /// <summary>
    /// 设置光标状态
    /// </summary>
    public void SetCursorState(CursorState newState)
    {
        if (currentState == newState) return;

        if (stateDictionary.TryGetValue(newState, out CursorStateData data))
        {
            Cursor.SetCursor(data.texture, data.hotspot, data.mode);
            currentState = newState;
            Debug.Log($"Cursor changed to: {newState}");
        }
        else
        {
            Debug.LogWarning($"Cursor state '{newState}' not configured!");
        }
    }

    /// <summary>
    /// 获取当前光标状态
    /// </summary>
    public CursorState GetCurrentCursorState()
    {
        return currentState;
    }

    /// <summary>
    /// 恢复到之前的状态
    /// </summary>
    public void RestorePreviousState()
    {
        SetCursorState(CursorState.Default);
    }
    
    public void SetForbiddenCursor() => SetCursorState(CursorState.Forbidden);
    public void SetClickableCursor() => SetCursorState(CursorState.Clickable);
}