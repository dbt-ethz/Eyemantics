using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public InputActionReference imageGazeCapture = null;

    private float time = 0.0f;
    private float gazeInterval = 20f;

    private static ImageGazeInput imageGazeInput;
    private MagicLeapInputs mlInputs;
    private MagicLeapInputs.ControllerActions controllerActions;
    void Start()
    {
        imageGazeInput = GameObject.Find("/InputManager").GetComponent<ImageGazeInput>();
    }
    private void Awake()
    {
        //imageGazeCapture.action.started += triggerPress;
        mlInputs = new MagicLeapInputs();
        mlInputs.Enable();
        controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

        controllerActions.Trigger.performed += triggerPress;
    }
    private void OnDestroy()
    {
        //imageGazeCapture.action.started -= triggerPress;
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
