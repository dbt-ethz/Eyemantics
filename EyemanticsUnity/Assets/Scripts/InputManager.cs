using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public InputActionReference imageGazeCapture = null;

    private ImageInput imageInput;
    private GazeInput gazeInput;
    void Start()
    {
        imageInput = GameObject.Find("/InputManager").GetComponent<ImageInput>();
        gazeInput = GameObject.Find("/InputManager").GetComponent<GazeInput>();
    }
    private void Awake()
    {
        imageGazeCapture.action.started += CaptureImage;
        imageGazeCapture.action.started += CaptureGaze;
    }
    private void OnDestroy()
    {
        imageGazeCapture.action.started -= CaptureImage;
        imageGazeCapture.action.started -= CaptureGaze;
    }
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            imageInput.ImageCapture();
            gazeInput.OutputGazePos();
        }
    }
#endif
    private void CaptureImage(InputAction.CallbackContext context)
    {
        imageInput.ImageCapture();
    }
    private void CaptureGaze(InputAction.CallbackContext context)
    {
        gazeInput.OutputGazePos();
    }
}
