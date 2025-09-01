using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SettingsPanel : BasePanel
{
    [Header("组件配置")]
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;
    
    public Button backButton;
    public Button resetButton;

    private void Start()
    {
        bgmVolumeSlider.value = AudioManager.Instance.bgmVolumeFactor;
        sfxVolumeSlider.value = AudioManager.Instance.sfxVolumeFactor;
        
        bgmVolumeSlider.onValueChanged.AddListener(ChangeBgmVolume);
        sfxVolumeSlider.onValueChanged.AddListener(ChangeSfxVolume);

        backButton.onClick.AddListener(SaveSettings);
        resetButton.onClick.AddListener(ResetSettings);
    }
    
    private void ChangeMainVolume(float value)
    {
        AudioManager.Instance.ChangeMainVolume(value);
    }
    
    private void ChangeBgmVolume(float value)
    {
        AudioManager.Instance.ChangeBgmVolume(value);
    }
    
    private void ChangeSfxVolume(float value)
    {
        AudioManager.Instance.ChangeSfxVolume(value);
    }
    
    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("MainVolume", AudioManager.Instance.mainVolume);
        PlayerPrefs.SetFloat("BgmVolumeFactor", AudioManager.Instance.bgmVolumeFactor);
        PlayerPrefs.SetFloat("SfxVolumeFactor", AudioManager.Instance.sfxVolumeFactor);
        PlayerPrefs.Save();

        UIManager.Instance.ClosePanel(panelName);
        Debug.Log("Settings Saved!");
    }
    
    private void ResetSettings()
    {
        bgmVolumeSlider.value = 0.8f;
        sfxVolumeSlider.value = 0.8f;
        
        ChangeMainVolume(1f);
        ChangeBgmVolume(0.8f);
        ChangeSfxVolume(0.8f);
    }
}
