using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using System.Threading;
using UnityEngine.Rendering;
using System.Text;
using System.Linq;

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
    public MLCamera.IntrinsicCalibrationParameters cameraIntrinsics;
    [SerializeField, Tooltip("Desired width for the camera capture")]
    private int captureWidth = 2880;
    [SerializeField, Tooltip("Desired height for the camera capture")]
    private int captureHeight = 2160;
    //Cache the capture configure for later use.
    private MLCamera.CaptureConfig _captureConfig;

    public GameObject gazeDisplayPrefab;
    public float gazeInterval = 0.2f;
    public float imageCaptureInterval = 1f;

    private bool permissionGranted = false;
    private readonly MLPermissions.Callbacks camPermissionCallbacks = new MLPermissions.Callbacks();
    private readonly MLPermissions.Callbacks eyePermissionCallbacks = new MLPermissions.Callbacks();
    private float time = 0.0f;
    private float cameraCaptureTime = 0f;
    private MLCamera.Identifier _identifier = MLCamera.Identifier.CV;
    private bool _cameraDeviceAvailable = false;

    //[SerializeField, Tooltip("The UI to show the camera capture in YUV format")]
    //private RawImage _screenRenderYUV = null;
    [SerializeField, Tooltip("YUV shader")]
    private Shader _yuv2RgbShader;
    // the image textures for each channel Y, U, V
    private Texture2D[] _rawVideoTextureYuv = new Texture2D[3];
    private byte[] _yChannelBuffer;
    private byte[] _uChannelBuffer;
    private byte[] _vChannelBuffer;
    private static readonly string[] SamplerNamesYuv = new string[] { "_MainTex", "_UTex", "_VTex" };
    // the texture that will display our fina image
    private RenderTexture _renderTexture;
    private Texture2D _combinedTexture2D;
    private Material _yuvMaterial;
    private CommandBuffer _commandBuffer;
    private Texture _combinedTexture;

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
        cameraCaptureTime += Time.deltaTime;
        time += Time.deltaTime;
        if (time >= gazeInterval)
        {
            time = 0;
            EyeTracking();
        }

        //if (!(TCPServer.mask == null || TCPServer.mask.Length == 0)) {
        //    Debug.Log("Mask not empty!");
        //}

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
        while (!permissionGranted)
        {
            yield return new WaitForSeconds(1.0f);
        }
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
            connectContext.CamId = _identifier;
            //The MLCamera.Identifier.Main is the only camera that can access the virtual and mixed reality flags
            connectContext.Flags = MLCamera.ConnectFlag.CamOnly;
            connectContext.EnableVideoStabilization = true;

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
        //MLCamera.OutputFormat outputFormat = MLCamera.OutputFormat.YUV_420_888;
        MLCamera.OutputFormat outputFormat = MLCamera.OutputFormat.YUV_420_888;
        _captureConfig.CaptureFrameRate = MLCamera.CaptureFrameRate.None;
        _captureConfig.StreamConfigs = new MLCamera.CaptureStreamConfig[1];
        _captureConfig.StreamConfigs[0] = MLCamera.CaptureStreamConfig.Create( defaultCapability, outputFormat);
    }
    private void SetCameraCallbacks()
    {
        _camera.OnRawImageAvailable += RowImageAvailable;
        //_camera.OnRawImageAvailable += OnCaptureDataReceived;
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
        Vector3 init_vec = new Vector3(0f, 0f, 1f);
        gazeDisplayPrefab.transform.position = pos.position;
    }
    void RowImageAvailable(MLCamera.CameraOutput output, MLCamera.ResultExtras extras, MLCamera.Metadata metadataHandle)
    {
        
        if (cameraCaptureTime < imageCaptureInterval)
        {
            return;
        }
        cameraCaptureTime = 0;
        //if (output.Format == MLCamera.OutputFormat.YUV_420_888)
        //{
        //UpdateJPGTexture(output.Planes[0]);
        //}
        //MLResult result = MLCVCamera.GetFramePose(extras.VCamTimestamp, out Matrix4x4 cameraTransform);
        //if (result.IsOk)
        //{


        //cameraPos.position = new Vector3(cameraTransform[0, 3], cameraTransform[1, 3], cameraTransform[2, 3]);
        //cameraPos.rotation = cameraTransform.rotation;

        cameraPos.position = Camera.main.gameObject.transform.position;
        cameraPos.rotation = Camera.main.gameObject.transform.rotation;
        //TCPServer.mask = null;
        Debug.Log($"cam position: {cameraPos.position}\ncam rotation: {cameraPos.rotation}");
        cameraIntrinsics = extras.Intrinsics.Value;
        Debug.Log($"Camera Resolution: {cameraIntrinsics.Width} * {cameraIntrinsics.Height}");
        pixelPos = ViewportPointFromWorld(cameraIntrinsics, gazeDisplayPrefab.transform.position, cameraPos.position, cameraPos.rotation);
        //Debug.Log($"image dimention: {captureWidth} * {captureHeight}");
        Debug.Log($"gaze pos 2D: {pixelPos}");
        //}
        //else
        //{
        //    Debug.Log("failed to receive extrinsic!!");
        //}

        ReceiveAndCombineYUV(output, extras, metadataHandle);

        Debug.Log("Ready to send out img and gaze pos!");
        // Send Image to PC
        if (!TCPServer.communicating)
        {
            Debug.Log("called tcpserver");
            TCPServer.communicating = true;
            ThreadStart tc = new ThreadStart(TCPServer.Communication);
            TCPServer.commThread = new Thread(tc);
            TCPServer.commThread.Start();
        }
        Debug.Log($"captured image size after comm: {bytes.Length}");

    }
    private void ReceiveAndCombineYUV(MLCamera.CameraOutput output, MLCamera.ResultExtras extras, MLCamera.Metadata metadataHandle)
    {
        if (output.Format != MLCamera.OutputFormat.YUV_420_888) return;
        MLCamera.FlipFrameVertically(ref output);
        InitializeMaterial();
        // read data from y u v 3 channels
        UpdateYUVTextureChannel(ref _rawVideoTextureYuv[0], output.Planes[0], SamplerNamesYuv[0], ref _yChannelBuffer);
        UpdateYUVTextureChannel(ref _rawVideoTextureYuv[1], output.Planes[1], SamplerNamesYuv[1], ref _uChannelBuffer);
        UpdateYUVTextureChannel(ref _rawVideoTextureYuv[2], output.Planes[2], SamplerNamesYuv[2], ref _vChannelBuffer);

        //combine 3 channels into 1 render texture
        CombineYUVChannels2RGB(output);
        // render texture to texture 2D
        //_combinedTexture2D = ToTexture2D(_renderTexture);
        _combinedTexture2D = ExtendedTexureMethod.toTexture2D(_renderTexture);
        bytes = null;
        bytes = _combinedTexture2D.EncodeToJPG();
        Debug.Log($"captured image size: {bytes.Length}");
        //string dataString = bytes.Aggregate(new StringBuilder(), (sb, b) => sb.AppendFormat("{0:x2} ", b), sb => sb.AppendFormat("({0})", bytes.Length).ToString());
        //SaveBytesArrayLocal(dataString);
    }
    private void UpdateYUVTextureChannel(ref Texture2D channelTexture, MLCamera.PlaneInfo imagePlane, string samplerName, ref byte[] newTexureChannel)
    {
        int textureWidth = (int)imagePlane.Width;
        int textureHeight = (int)imagePlane.Height;
        if (channelTexture == null || channelTexture.width != textureWidth || channelTexture.height != textureHeight)
        {
            if (channelTexture != null)
            {
                Destroy(channelTexture);
            }
            if (imagePlane.PixelStride == 2)
            {
                channelTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RG16, false);
            }
            else
            {
                channelTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.Alpha8, false);
            }
            channelTexture.filterMode = FilterMode.Bilinear;
            _yuvMaterial.SetTexture(samplerName, channelTexture);
        }

        int actualWidth = (int)(textureWidth * imagePlane.PixelStride);

        if (imagePlane.Stride != actualWidth)
        {
            int requiredLength = actualWidth * textureHeight;
            if (newTexureChannel == null || newTexureChannel.Length != requiredLength)
            {
                newTexureChannel = new byte[requiredLength];
            }
            for (int i = 0; i < textureHeight; i++)
            {
                int sourceOffset = (int)(i * imagePlane.Stride);
                int destOffset = i * actualWidth;
                Buffer.BlockCopy(imagePlane.Data, sourceOffset, newTexureChannel, destOffset, actualWidth);
            }
            channelTexture.LoadRawTextureData(newTexureChannel);
        }
        else
        {
            channelTexture.LoadRawTextureData(imagePlane.Data);
        }
        channelTexture.Apply();
    }
    private void CombineYUVChannels2RGB(MLCamera.CameraOutput output)
    {
        if (!_renderTexture)
        {
            _renderTexture = new RenderTexture((int)output.Planes[0].Width, (int)output.Planes[0].Height, 0, RenderTextureFormat.ARGB32, 0);
            if (_commandBuffer == null)
            {
                _commandBuffer = new CommandBuffer();
                _commandBuffer.name = "YUV2RGB";
            }
            //_screenRenderYUV.texture = _renderTexture;
        }

        _yuvMaterial.mainTextureScale = new Vector2(1f / output.Planes[0].PixelStride, 1.0f);
        _commandBuffer.Blit(null, _renderTexture, _yuvMaterial);
        Graphics.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }
    private Texture2D ToTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }
    private void SaveBytesArrayLocal(string dataString)
    {
        string path = Application.persistentDataPath + "/" + "bytesArray";
        Debug.Log($"save to local: {path}");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        StreamWriter writer = new StreamWriter(path + "/bytes.text");
        writer.Write(dataString);
        writer.Flush();
        writer.Close();
    }
    private void InitializeMaterial()
    {
        if(_yuv2RgbShader == null)
        {
            _yuv2RgbShader = Shader.Find("Unlit/YUV_Camera_Shader");
            if(_yuv2RgbShader == null)
            {
                Debug.LogError("shader not found!");
                return;
            }
        }
       if(_yuvMaterial == null)
        {
            _yuvMaterial = new Material(_yuv2RgbShader);
        }
    }
    private void UpdateJPGTexture(MLCamera.PlaneInfo imagePlane)
    {
        Debug.Log($"read image plane data: {imagePlane.Data.Length}");
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
        bytes = image.EncodeToJPG();
        Destroy(image);
        Debug.Log($"save image size: {bytes.Length}");
        string dataString = bytes.Aggregate(new StringBuilder(), (sb, b) => sb.AppendFormat("{0:x2} ", b), sb => sb.AppendFormat("({0})", bytes.Length).ToString());
        SaveBytesArrayLocal(dataString);

#if UNITY_EDITOR
        File.WriteAllBytes(Application.dataPath + "/Images/" + Time.time + ".jpgs", bytes);
#endif
    }
    public Vector2 ViewportPointFromWorld(MLCamera.IntrinsicCalibrationParameters icp, Vector3 worldPoint, Vector3 cameraPos, Quaternion cameraRotation)
    {

        // Step 1: Convert world point to camera space 
        Vector3 pointInCameraSpace = Quaternion.Inverse(cameraRotation) * (worldPoint - cameraPos);

        //Debug.Log($"3D Point in Camera Space: {pointInCameraSpace}");

        // Step 2: Project the point onto the image plane using the camera intrinsics
        if (pointInCameraSpace.z <= 0) // Avoid division by zero
            return new Vector2(-1f, -1f); // Indicate an error or out-of-bounds

        // Step 2: Project the camera-space point onto the image plane
        Vector2 viewportPoint = new Vector2(
            icp.FocalLength.x * pointInCameraSpace.x / pointInCameraSpace.z + icp.PrincipalPoint.x,
            icp.Height - ((pointInCameraSpace.y / pointInCameraSpace.z) * icp.FocalLength.y + icp.PrincipalPoint.y)
        );

        return viewportPoint;
        


        //float x = (pointInCameraSpace.x / pointInCameraSpace.z) * icp.FocalLength.x + icp.PrincipalPoint.x;
        //float y = icp.Height - ((pointInCameraSpace.y / pointInCameraSpace.z) * icp.FocalLength.y + icp.PrincipalPoint.y);
        //Vector2 viewportPoint = new Vector2(x, y);

        //return viewportPoint;
    }
}
