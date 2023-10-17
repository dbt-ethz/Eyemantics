using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class EyeTracking : MonoBehaviour
{
    private bool permissionGranted = false;
    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();
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
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OutputEyeTracking();
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

    private void OutputEyeTracking()
    {
        if (!permissionGranted)
        {
            return;
        }

        MLResult gazeStateResult = MLGazeRecognition.GetState(out MLGazeRecognition.State state);
        MLResult gazeStaticDataResult = MLGazeRecognition.GetStaticData(out MLGazeRecognition.StaticData data);

        Debug.Log($"MLGazeRecognitionStaticData {gazeStaticDataResult.Result}\n" +
            $"Vergence {data.Vergence}\n" +
            $"EyeHeightMax {data.EyeHeightMax}\n" +
            $"EyeWidthMax {data.EyeWidthMax}\n" +
            $"MLGazeRecognitionState: {gazeStateResult.Result}\n" +
            state.ToString());
    }
}
