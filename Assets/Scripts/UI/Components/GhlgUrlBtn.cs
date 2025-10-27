using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GhlgUrlBtn : MonoBehaviour
{
    [SerializeField] private string url = "https://ghlg.fun";
    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        
        button.onClick.AddListener(OpenURL);
    }

    private void OpenURL()
    {
        Application.OpenURL(url);
    }
}