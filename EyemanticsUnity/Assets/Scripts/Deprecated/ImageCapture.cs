using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ImageCapture : MonoBehaviour
{
    public int resWidth = 1080;
    public int resHeight = 900;

    private Camera mainCam;
    private int fileCounter = 0;

    void Start()
    {
        mainCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            CamCapture();
        }
    }
    public void CamCapture()
    {
        Debug.Log("save cam image");
        RenderTexture renderTexture = new RenderTexture(resWidth, resHeight, 24);
        mainCam.targetTexture = renderTexture;
        mainCam.Render();
        RenderTexture.active = renderTexture;

        Texture2D image = new Texture2D(resWidth, resHeight);
        image.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        //image.Apply();

        mainCam.targetTexture = null;
        RenderTexture.active = null;

        byte[] bytes = image.EncodeToPNG();
        Destroy(image);

        File.WriteAllBytes(Application.dataPath + "/Images/" + Time.time + ".png", bytes);
        fileCounter++;
    }
}
