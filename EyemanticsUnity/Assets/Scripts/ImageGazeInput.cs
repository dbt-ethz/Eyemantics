using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using System.Threading;

public class ImageGazeInput : MonoBehaviour
{
    [HideInInspector]
    public byte[] bytes = null;
    [HideInInspector]
    public Pose cameraPos;
    [HideInInspector]
    public Vector2 pixelPos;

    private Texture2D _imageTexture;
    public MLCamera _camera;
    [SerializeField, Tooltip("Desired width for the camera capture")]
    private int captureWidth = 1280;
    [SerializeField, Tooltip("Desired height for the camera capture")]
    private int captureHeight = 720;
    //Cache the capture configure for later use.
    private MLCamera.CaptureConfig _captureConfig;

    public GameObject gazeDisplayPrefab;
    public float gazeInterval = 0.2f;
    private bool permissionGranted = false;
    private readonly MLPermissions.Callbacks camPermissionCallbacks = new MLPermissions.Callbacks();
    private readonly MLPermissions.Callbacks eyePermissionCallbacks = new MLPermissions.Callbacks();
    private float time = 0.0f;
    private MLCamera.Identifier _identifier = MLCamera.Identifier.Main;
    private bool _cameraDeviceAvailable = false;
    private void Awake()
    {
        camPermissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        camPermissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        camPermissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
        MLPermissions.RequestPermission(MLPermission.Camera, camPermissionCallbacks);
        MLPermissions.RequestPermission(MLPermission.EyeTracking, eyePermissionCallbacks);
    }
    private void OnDestroy()
    {
        camPermissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        camPermissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        camPermissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
    }
    private void Start()
    {
        StartCoroutine(EnableMLCamera());
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
        Debug.Log($"{permission} denied. The example will not function as expected.");
    }
    private void OnPermissionGranted(string permission)
    {
        permissionGranted = true;
        Debug.Log($"{permission} granted. The example will function as expected.");
    }
    private IEnumerator EnableMLCamera()
    {
        while (!_cameraDeviceAvailable)
        {
            MLResult result = MLCamera.GetDeviceAvailabilityStatus(_identifier, out _cameraDeviceAvailable);
            if(result.IsOk == false || _cameraDeviceAvailable == false)
            {
                yield return new WaitForSeconds(1.0f);
            }
            Debug.Log("tring to connnect camera...");
        }
        ConnectCamera();
    }
    private void ConnectCamera()
    {
        if (_cameraDeviceAvailable)
        {
            MLCamera.ConnectContext connectContext = MLCamera.ConnectContext.Create();
            _camera = MLCamera.CreateAndConnect(connectContext);
            if (_camera != null)
            {
                Debug.Log("Camera device connected");
                ConfigureCameraInput();
                SetCameraCallbacks();
            }
            else
            {
                Debug.Log("failed to connect camera");
            }
        }
    }
    private void ConfigureCameraInput()
    {
        MLCamera.StreamCapability[] streamCapabilities = MLCamera.GetImageStreamCapabilitiesForCamera(_camera, MLCamera.CaptureType.Image);

        if (streamCapabilities.Length == 0)
            return;

        MLCamera.StreamCapability defaultCapability = streamCapabilities[0];

        if (MLCamera.TryGetBestFitStreamCapabilityFromCollection(streamCapabilities, captureWidth, captureHeight,
                MLCamera.CaptureType.Image, out MLCamera.StreamCapability selectedCapability))
        {
            defaultCapability = selectedCapability;
        }

        _captureConfig = new MLCamera.CaptureConfig();
        MLCamera.OutputFormat outputFormat = MLCamera.OutputFormat.YUV_420_888;
        _captureConfig.CaptureFrameRate = MLCamera.CaptureFrameRate.None;
        _captureConfig.StreamConfigs = new MLCamera.CaptureStreamConfig[1];
        _captureConfig.StreamConfigs[0] = MLCamera.CaptureStreamConfig.Create( defaultCapability, outputFormat);
    }
    private void SetCameraCallbacks()
    {
        _camera.OnRawImageAvailable += RowImageAvailable;
    }
    public void ImageCapture()
    {
        if (!permissionGranted)
        {
            return;
        }
        MLResult result = _camera.PrepareCapture(_captureConfig, out MLCamera.Metadata metaData);
        if (result.IsOk)
        {
            //Debug.Log("first result ok!!");
            // Trigger auto exposure and auto white balance
            _camera.PreCaptureAEAWB();
            result = _camera.CaptureImage();
            if (result.IsOk)
            {
                Debug.Log("image captured!!");
                //PopOutInfo.Instance.AddText("image captured!!");
            }
            else
            {
                Debug.LogError("Failed to start image capture!");
                //PopOutInfo.Instance.AddText("Failed to start image capture!");
            }
        }
        //else
        //{
        //    Debug.Log("first result not ok!!");
        //}
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
    void RowImageAvailable(MLCamera.CameraOutput output, MLCamera.ResultExtras extras, MLCamera.Metadata metadataHandle)
    {

        //if (output.Format == MLCamera.OutputFormat.YUV_420_888)
        //{
        SaveYUVData(output.Planes[0]);
        //UpdateJPGTexture(output.Planes[0]);
        //}
        MLResult result = MLCVCamera.GetFramePose(extras.VCamTimestamp, out Matrix4x4 cameraTransform);
        if (result.IsOk)
        {
            cameraPos.position = new Vector3(cameraTransform[0, 3], cameraTransform[1, 3], cameraTransform[2, 3]);
            cameraPos.rotation = cameraTransform.rotation;
            //TODO test viewport point from world function
            pixelPos = new Vector2(captureWidth/2, captureHeight/2);
            Debug.Log($"cam position: {cameraPos.position}\ncam rotation: {cameraPos.rotation}");
            pixelPos = ViewportPointFromWorld(extras.Intrinsics.Value, gazeDisplayPrefab.transform.position, cameraPos.position, cameraPos.rotation);
            Debug.Log($"image pixel: {captureWidth} * {captureHeight}");
            Debug.Log($"gaz pos 2D: {pixelPos}");
        }
        else
        {
            Debug.Log("failed to receive extrinsic!!");
        }
        PopOutInfo.Instance.AddText("Ready to send out img and gaze pos!");
        // Send Image to PC
        if (!TCPServer.communicating)
        {
            PopOutInfo.Instance.AddText("called tcpserver");
            TCPServer.communicating = true;
            ThreadStart tc = new ThreadStart(TCPServer.Communication);
            TCPServer.commThread = new Thread(tc);
            TCPServer.commThread.Start();
        }
    }
    private void SaveYUVData(MLCamera.PlaneInfo imagePlane)
    {
        bytes = imagePlane.Data;
        Debug.Log($"image size: {bytes.Length}");
    }
//    private void UpdateJPGTexture(MLCamera.PlaneInfo imagePlane)
//    {
//        Debug.Log($"read image plane data: {imagePlane.Data.Length}");
//        if (_imageTexture != null)
//        {
//            Destroy(_imageTexture);
//        }

//        _imageTexture = new Texture2D(8, 8);
//        bool status = _imageTexture.LoadImage(imagePlane.Data);
//        if (status && (_imageTexture.width != 8 && _imageTexture.height != 8))
//        {
//            SaveTexture(_imageTexture, captureWidth, captureHeight);
//        }
//    }
//    private void SaveTexture(Texture2D image, int resWidth, int resHeight)
//    {
//        bytes = image.EncodeToPNG();
//        Destroy(image);
//        Debug.Log($"image size: {bytes.Length}");
//#if UNITY_EDITOR
//        File.WriteAllBytes(Application.dataPath + "/Images/" + Time.time + ".png", bytes);
//#endif
//    }
    public Vector2 ViewportPointFromWorld(MLCamera.IntrinsicCalibrationParameters icp, Vector3 worldPoint, Vector3 cameraPos, Quaternion cameraRotation)
    {
        // Step 1: Convert world point to camera space
        Vector3 pointInCameraSpace = cameraRotation * (worldPoint - cameraPos);
        //Debug.Log($"world pos: {worldPoint}");
        //Debug.Log($"cam pos: {pointInCameraSpace}");
        // Step 2: Project the point onto the image plane using the camera intrinsics
        if (pointInCameraSpace.z == 0) // Avoid division by zero
            return new Vector2(-1, -1); // Indicate an error or out-of-bounds

        float x = (pointInCameraSpace.x / pointInCameraSpace.z) * icp.FocalLength.x + icp.PrincipalPoint.x;
        float y = icp.Height - ((pointInCameraSpace.y / pointInCameraSpace.z) * icp.FocalLength.y + icp.PrincipalPoint.y);
        Vector2 viewportPoint = new Vector2(x, y);
        //Debug.Log($"projecting pos: {viewportPoint}");
        // Step 3: Convert to viewport coordinates
        //Vector2 viewportPoint = new Vector2(x / icp.Width, y / icp.Height);
        return viewportPoint;
    }
}
