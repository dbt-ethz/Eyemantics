using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopOutInfo : MonoBehaviour
{
    public static PopOutInfo Instance;

    public TextMeshPro text;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            if (Instance == this) return;
            Destroy(Instance.gameObject);
            Instance = this;
        }
        this.text.text = "default text";
        Application.logMessageReceived += HandleLog;
    }
    public void AddText(string newText)
    {
        if(this.text.text.Length > 2000)
        {
            this.text.text = "";
        } 
        this.text.text += '\n';
        this.text.text += newText;
    }
    private void HandleLog(string message, string stackTrace, LogType type)
    {
        this.AddText(message);
    }
}
