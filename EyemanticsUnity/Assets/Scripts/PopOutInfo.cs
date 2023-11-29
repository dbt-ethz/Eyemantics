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
    }

    private void Start()
    {
        this.text.text = "default text";
    }

    public void AddText(string newText)
    {
        if(this.text.text.Length > 200)
        {
            this.text.text = "";
        } 
        this.text.text += '\n';
        this.text.text += newText;
    }
}
