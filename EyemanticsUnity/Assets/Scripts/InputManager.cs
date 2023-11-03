using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public InputActionReference imageGazeCapture = null;

    private ImageGazeInput imageGazeInput;
    void Start()
    {
        imageGazeInput = GameObject.Find("/InputManager").GetComponent<ImageGazeInput>();
    }
    private void Awake()
    {
        imageGazeCapture.action.started += CaptureImageGaze;
    }
    private void OnDestroy()
    {
        imageGazeCapture.action.started -= CaptureImageGaze;
    }
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            imageGazeInput.ImageCapture();
        }
    }
#endif
    private void CaptureImageGaze(InputAction.CallbackContext context)
    {
        imageGazeInput.ImageCapture();
    }
}
