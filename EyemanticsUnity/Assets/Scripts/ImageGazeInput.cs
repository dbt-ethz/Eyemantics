using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

public class ImageGazeInput : MonoBehaviour
{
    [HideInInspector]
    public byte[] bytes = null;
    [HideInInspector]
    public Pose cameraPos;
    [HideInInspector]
    public Vector2 pixelPos;

    private Texture2D _imageTexture;
    private MLCamera _camera;
    [SerializeField, Tooltip("Desired width for the camera capture")]
    private int captureWidth = 1280;
    [SerializeField, Tooltip("Desired height for the camera capture")]
    private int captureHeight = 720;
    //Cache the capture configure for later use.
    private MLCamera.CaptureConfig _captureConfig;

    public GameObject gazeDisplayPrefab;
    public float gazeInterval = 0.2f;
    private bool permissionGranted = false;
    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();
    private float time = 0.0f;
    private void Awake()
    {
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

        MLPermissions.RequestPermission(MLPermission.EyeTracking, permissionCallbacks);
    }
    private void OnDestroy()
    {
        permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
    }
    private void Start()
    {
        ConnectCamera();
    }
    private void Update()
    {

        time += Time.deltaTime;
        if (time >= gazeInterval)
        {
            time = 0;
            EyeTracking();
        }
    }
    private void OnPermissionDenied(string permission)
    {
        MLPluginLog.Error($"{permission} denied, test won't function.");
    }
    private void OnPermissionGranted(string permission)
    {
        permissionGranted = true;
    }
    private void EyeTracking()
    {
        if (!permissionGranted)
        {
            return;
        }

        MLResult gazeStateResult = MLGazeRecognition.GetState(out MLGazeRecognition.State state);
        MLResult gazeStaticDataResult = MLGazeRecognition.GetStaticData(out MLGazeRecognition.StaticData data);

        //Debug.Log($"MLGazeRecognitionStaticData {gazeStaticDataResult.Result}\n" +
        //    $"Vergence {data.Vergence}\n" +
        //    $"EyeHeightMax {data.EyeHeightMax}\n" +
        //    $"EyeWidthMax {data.EyeWidthMax}\n" +
        //    $"MLGazeRecognitionState: {gazeStateResult.Result}\n" +
        //    state.ToString());
        if (data.Vergence != null)
        {
            UpdateSphere(data.Vergence);
        }
    }
    private void UpdateSphere(Pose pos)
    {
        gazeDisplayPrefab.transform.position = pos.position;
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
    public void ImageCapture()
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
        if(MLCVCamera.GetFramePose(extras.VCamTimestamp, out Matrix4x4 cameraTransform).IsOk)
        {
            cameraPos.position = new Vector3(cameraTransform[0, 3], cameraTransform[1, 3], cameraTransform[2, 3]);
            cameraPos.rotation = cameraTransform.rotation;
            pixelPos = ViewportPointFromWorld(extras.Intrinsics.Value, gazeDisplayPrefab.transform.position, cameraPos.position, cameraPos.rotation);
            Debug.Log(pixelPos);
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
            SaveTexture(_imageTexture, captureWidth, captureHeight);
        }
    }
    private void SaveTexture(Texture2D image, int resWidth, int resHeight)
    {
        bytes = image.EncodeToPNG();
        Destroy(image);
#if UNITY_EDITOR
        File.WriteAllBytes(Application.dataPath + "/Images/" + Time.time + ".png", bytes);
#endif
    }
    public Vector2 ViewportPointFromWorld(MLCamera.IntrinsicCalibrationParameters icp, Vector3 worldPoint, Vector3 cameraPos, Quaternion cameraRotation)
    {
        // Step 1: Convert world point to camera space
        Vector3 pointInCameraSpace = cameraRotation * (worldPoint - cameraPos);

        // Step 2: Project the point onto the image plane using the camera intrinsics
        if (pointInCameraSpace.z == 0) // Avoid division by zero
            return new Vector2(-1, -1); // Indicate an error or out-of-bounds

        float x = (pointInCameraSpace.x / pointInCameraSpace.z) * icp.FocalLength.x + icp.PrincipalPoint.x;
        float y = icp.Height - ((pointInCameraSpace.y / pointInCameraSpace.z) * icp.FocalLength.y + icp.PrincipalPoint.y);

        // Step 3: Convert to viewport coordinates
        Vector2 viewportPoint = new Vector2(x / icp.Width, y / icp.Height);

        return viewportPoint;
    }
}
