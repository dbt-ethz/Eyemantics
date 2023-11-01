using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

public class CameraRendering : MonoBehaviour
{
    //[SerializeField, Tooltip("The UI to show the camera capture in JPEG format")]
    //private RawImage _screenRendererJPEG = null;
    private Texture2D _imageTexture;
    private MLCamera _camera;
    [SerializeField, Tooltip("Desired width for the camera capture")]
    private int captureWidth = 1280;
    [SerializeField, Tooltip("Desired height for the camera capture")]
    private int captureHeight = 720;
    //Cache the capture configure for later use.
    private MLCamera.CaptureConfig _captureConfig;

    private void Start()
    {
        ConnectCamera();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ImageCapture();
        }
    }
    private void ConnectCamera()
    {
        MLCamera.ConnectContext connectContext = MLCamera.ConnectContext.Create();
        _camera = MLCamera.CreateAndConnect(connectContext);
        if (_camera != null)
        {
            Debug.Log("Camera device connected");
            ConfigureCameraInput();
            SetCameraCallbacks();
        }
    }
    private void ConfigureCameraInput()
    {
        //Gets the stream capabilities the selected camera. (Supported capture types, formats and resolutions)
        MLCamera.StreamCapability[] streamCapabilities = MLCamera.GetImageStreamCapabilitiesForCamera(_camera, MLCamera.CaptureType.Image);

        if (streamCapabilities.Length == 0)
            return;

        //Set the default capability stream
        MLCamera.StreamCapability defaultCapability = streamCapabilities[0];

        //Try to get the stream that most closely matches the target width and height
        if (MLCamera.TryGetBestFitStreamCapabilityFromCollection(streamCapabilities, captureWidth, captureHeight,
                MLCamera.CaptureType.Image, out MLCamera.StreamCapability selectedCapability))
        {
            defaultCapability = selectedCapability;
        }

        //Initialize a new capture config.
        _captureConfig = new MLCamera.CaptureConfig();
        //Set RGBA video as the output
        MLCamera.OutputFormat outputFormat = MLCamera.OutputFormat.JPEG;
        //Set the Frame Rate as none for image capturing
        _captureConfig.CaptureFrameRate = MLCamera.CaptureFrameRate.None;
        //Initialize a camera stream config.
        //The Main Camera can support up to two stream configurations
        _captureConfig.StreamConfigs = new MLCamera.CaptureStreamConfig[1];
        _captureConfig.StreamConfigs[0] = MLCamera.CaptureStreamConfig.Create(
            defaultCapability, outputFormat
        );
    }
    private void ImageCapture()
    {
        MLResult result = _camera.PrepareCapture(_captureConfig, out MLCamera.Metadata metaData);
        if (result.IsOk)
        {
            // Trigger auto exposure and auto white balance
            _camera.PreCaptureAEAWB();
            result = _camera.CaptureImage();
            if (result.IsOk)
            {
                Debug.Log("capture image!");
            }
            else
            {
                Debug.LogError("Failed to start image capture!");
            }
        }
    }
    private void SetCameraCallbacks()
    {
        _camera.OnRawImageAvailable += RowImageAvailable;
        Debug.Log("set camera call backs");
    }
    void RowImageAvailable(MLCamera.CameraOutput output, MLCamera.ResultExtras extras, MLCamera.Metadata metadataHandle)
    {
        if (output.Format == MLCamera.OutputFormat.JPEG)
        {
            UpdateJPGTexture(output.Planes[0]);
        }
    }
    private void UpdateJPGTexture(MLCamera.PlaneInfo imagePlane)
    {
        if (_imageTexture != null)
        {
            Destroy(_imageTexture);
        }

        _imageTexture = new Texture2D(8, 8);
        bool status = _imageTexture.LoadImage(imagePlane.Data);
        if (status && (_imageTexture.width != 8 && _imageTexture.height != 8))
        {
            //_screenRendererJPEG.texture = _imageTexture;
            SaveTexture(_imageTexture, captureWidth, captureHeight);
        }
    }
    private void SaveTexture(Texture2D image, int resWidth, int resHeight)
    {
        byte[] bytes = image.EncodeToPNG();
        Destroy(image);

        File.WriteAllBytes(Application.dataPath + "/Images/" + Time.time + ".png", bytes);
    }
    private void Test()
    {
 
    }
}
