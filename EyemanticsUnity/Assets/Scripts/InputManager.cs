using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private static ImageGazeInput imageGazeInput;
    private MagicLeapInputs mlInputs;
    private MagicLeapInputs.ControllerActions controllerActions;

    private static float cameraCaptureTime = 0f;
    private static float imageCaptureInterval = 1f;
    private void Awake()
    {
        mlInputs = new MagicLeapInputs();
        mlInputs.Enable();
        controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);
        controllerActions.Trigger.performed += triggerPress;
    }
    private void Start()
    {
        imageGazeInput = GameObject.Find("/InputManager").GetComponent<ImageGazeInput>();
    }

    private void Update()
    {

        cameraCaptureTime += Time.deltaTime;
    }

    private void OnDestroy()
    {
        controllerActions.Trigger.performed -= triggerPress;
    }
    public static void triggerPress(InputAction.CallbackContext context)
    {

        if (cameraCaptureTime < imageCaptureInterval)
        {
            return;
        }
        cameraCaptureTime = 0;

        PopOutInfo.Instance.AddText("trigger press!!");
        imageGazeInput.ImageCapture();
    }
}
