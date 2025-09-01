using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PausePanel : BasePanel
{
    [Header("组件配置")]
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;
    
    public Button resumeButton;
    public Button mainmenuButton;

    private void Start()
    {
        bgmVolumeSlider.value = AudioManager.Instance.bgmVolumeFactor;
        sfxVolumeSlider.value = AudioManager.Instance.sfxVolumeFactor;
        
        bgmVolumeSlider.onValueChanged.AddListener(ChangeBgmVolume);
        sfxVolumeSlider.onValueChanged.AddListener(ChangeSfxVolume);

        resumeButton.onClick.AddListener(OnResumeButtonClick);
        mainmenuButton.onClick.AddListener(OnMainmenuButtonClick);
        
        GameManager.Instance.PauseGame();
    }

    #region 音量设置

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
        
        Debug.Log("Settings Saved!");
    }

    #endregion

    private void OnMainmenuButtonClick()
    {
        SaveSettings();
        GameManager.Instance.ReturnToMainMenu();
    }

    private void OnResumeButtonClick()
    {
        SaveSettings();
        GameManager.Instance.ResumeGame();
        UIManager.Instance.ClosePanel(panelName);
    }
}
