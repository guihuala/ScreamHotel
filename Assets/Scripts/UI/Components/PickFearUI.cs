using System;
using UnityEngine;
using UnityEngine.UI;
using ScreamHotel.Domain;

public class PickFearUI : MonoBehaviour
{
    public RectTransform panel;
    public Button btnDarkness, btnBlood, btnNoise, btnRot, btnGaze;

    private Action<FearTag> _onPick;

    public void Init(Action<FearTag> onPick)
    {
        _onPick = onPick;
        // 这里省略实例化按钮的代码，你可以在预制体里排好五个按钮并拖引用
        btnDarkness.onClick.AddListener(() => Pick(FearTag.Darkness));
        btnBlood.onClick.AddListener(() => Pick(FearTag.Blood));
        btnNoise.onClick.AddListener(() => Pick(FearTag.Noise));
        btnRot.onClick.AddListener(() => Pick(FearTag.Rot));
        btnGaze.onClick.AddListener(() => Pick(FearTag.Gaze));
    }

    private void Pick(FearTag t)
    {
        _onPick?.Invoke(t);
        Destroy(gameObject); // 选完即关
    }
}