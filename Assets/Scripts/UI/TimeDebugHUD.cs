using UnityEngine;
using ScreamHotel.Core;
using ScreamHotel.Systems;

public class TimeDebugHUD : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.F3;
    private bool _show = true;
    private Rect _rect = new Rect(10, 10, 280, 200);

    private Game _game;
    private TimeSystem TS => _game?.TimeSystem;

    void Awake() { _game = FindObjectOfType<Game>(); }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) _show = !_show;
    }

    void OnGUI()
    {
        if (!_show || _game == null || TS == null) return;
        _rect = GUI.Window(9527, _rect, Draw, "Time Debug");
    }

    void Draw(int id)
    {
        GUILayout.Label($"State: {_game.State}   Day: {_game.DayIndex}");
        GUILayout.Label($"t: {TS.currentTimeOfDay:0.000}   {(TS.IsNight ? "Night" : "Day")}");
        GUILayout.Label($"Paused: {TS.isPaused}   DayLen(s): {TS.dayDurationInSeconds:0}");

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("⏯ Pause/Resume")) TS.isPaused = !TS.isPaused;
        if (GUILayout.Button("⏩ x2 Speed")) TS.dayDurationInSeconds = Mathf.Max(10f, TS.dayDurationInSeconds / 2f);
        if (GUILayout.Button("⏪ /2 Speed")) TS.dayDurationInSeconds = Mathf.Min(3600f, TS.dayDurationInSeconds * 2f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("↦ Dawn 0.25")) TS.SetNormalizedTime(0.25f);
        if (GUILayout.Button("↦ Noon 0.50")) TS.SetNormalizedTime(0.50f);
        if (GUILayout.Button("↦ Dusk 0.75")) TS.SetNormalizedTime(0.75f);
        if (GUILayout.Button("↦ Mid 0.00")) TS.SetNormalizedTime(0.00f);
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        if (GUILayout.Button("Start Night Show")) _game.StartNightShow();
        if (GUILayout.Button("Exec Night (seed=42)")) _game.StartNightExecution(42);

        GUI.DragWindow();
    }
}