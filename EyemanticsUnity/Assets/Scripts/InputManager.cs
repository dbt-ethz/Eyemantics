using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private static ImageGazeInput imageGazeInput;
    private MagicLeapInputs mlInputs;
    private MagicLeapInputs.ControllerActions controllerActions;
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
    private void OnDestroy()
    {
        controllerActions.Trigger.performed -= triggerPress;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Return))
        {
            imageGazeInput.ImageCapture();
        }
#endif
    }
    public static void triggerPress(InputAction.CallbackContext context)
    {
        PopOutInfo.Instance.AddText("trigger press!!");
        imageGazeInput.ImageCapture();
    }
}
