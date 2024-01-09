
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.InteractionSubsystems;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.Management;


/// <summary>
/// This represents all the runtime control over meshing component in order to best visualize the
/// affect changing parameters has over the meshing API.
/// </summary>
public class MeshingMain : MonoBehaviour
{
    [SerializeField, Tooltip("The spatial mapper from which to update mesh params.")]
    private MeshingSubsystemComponent _meshingSubsystemComponent = null;

    [SerializeField, Tooltip("Visualizer for the meshing results.")]
    private MeshingVisualizer _meshingVisualizer = null;

    [SerializeField, Space, Tooltip("A visual representation of the meshing bounds.")]
    private GameObject _visualBounds = null;

    [SerializeField, Space, Tooltip("Flag specifying if mesh extents are bounded.")]
    private bool _bounded = false;

    [SerializeField, Space, Tooltip("Size of the bounds extents when bounded setting is enabled.")]
    private Vector3 _boundedExtentsSize = new Vector3(2.0f, 2.0f, 2.0f);

    [SerializeField, Space, Tooltip("Size of the bounds extents when bounded setting is disabled.")]
    private Vector3 _boundlessExtentsSize = new Vector3(10.0f, 10.0f, 10.0f);

    private Camera _camera = null;

    private MagicLeapInputs mlInputs;
    private MagicLeapInputs.ControllerActions controllerActions;
    private XRInputSubsystem inputSubsystem;

    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();


    /// <summary>
    /// Initializes component data and starts MLInput.
    /// </summary>
    void Awake()
    {
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

        if (_meshingSubsystemComponent == null)
        {
            Debug.LogError("MeshingExample._meshingSubsystemComponent is not set. Disabling script.");
            enabled = false;
            return;
        }
        else
        {
            // disable _meshingSubsystemComponent until we have successfully requested permissions
            _meshingSubsystemComponent.enabled = false;
        }
        if (_meshingVisualizer == null)
        {
            Debug.LogError("MeshingExample._meshingVisualizer is not set. Disabling script.");
            enabled = false;
            return;
        }
        if (_visualBounds == null)
        {
            Debug.LogError("MeshingExample._visualBounds is not set. Disabling script.");
            enabled = false;
            return;
        }

        MLDevice.RegisterGestureSubsystem();
        if (MLDevice.GestureSubsystemComponent == null)
        {
            Debug.LogError("MLDevice.GestureSubsystemComponent is not set. Disabling script.");
            enabled = false;
            return;
        }

        _camera = Camera.main;

        mlInputs = new MagicLeapInputs();
        mlInputs.Enable();
        controllerActions = new MagicLeapInputs.ControllerActions(mlInputs);

        controllerActions.Trigger.performed += InputManager.triggerPress;
        controllerActions.Bumper.performed += OnBumperDown;
        controllerActions.Menu.performed += OnMenuDown;

    }

    /// <summary>
    /// Set correct render mode for meshing and update meshing settings.
    /// </summary>
    private void Start()
    {
        MLPermissions.RequestPermission(MLPermission.SpatialMapping, permissionCallbacks);
        var xrMgrSettings = XRGeneralSettings.Instance.Manager;
        if (xrMgrSettings != null)
        {
            var loader = xrMgrSettings.activeLoader;
            if (loader != null)
            {
                inputSubsystem = loader.GetLoadedSubsystem<XRInputSubsystem>();
                inputSubsystem.trackingOriginUpdated += OnTrackingOriginChanged;

                _meshingSubsystemComponent.gameObject.transform.position = _camera.gameObject.transform.position;
                UpdateBounds();
            }
        }
    }

    /// <summary>
    /// Update mesh polling center position to camera.
    /// </summary>
    void Update()
    {

        _meshingSubsystemComponent.gameObject.transform.position = _camera.gameObject.transform.position;
        if ((_bounded && _meshingSubsystemComponent.gameObject.transform.localScale != _boundedExtentsSize) ||
            (!_bounded && _meshingSubsystemComponent.gameObject.transform.localScale != _boundlessExtentsSize))
        {
            UpdateBounds();
        }

    }

    /// <summary>
    /// Cleans up the component.
    /// </summary>
    void OnDestroy()
    {
        permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;

        controllerActions.Bumper.performed -= OnBumperDown;
        controllerActions.Menu.performed -= OnMenuDown;
        inputSubsystem.trackingOriginUpdated -= OnTrackingOriginChanged;

        mlInputs.Dispose();
    }

    private void OnPermissionGranted(string permission)
    {
        _meshingSubsystemComponent.enabled = true;
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Failed to create Meshing Subsystem due to missing or denied {MLPermission.SpatialMapping} permission. Please add to manifest. Disabling script.");
        enabled = false;
        _meshingSubsystemComponent.enabled = false;
    }

    /// <summary>
    /// Handles the event for bumper down. Changes render mode.
    /// </summary>
    /// <param name="callbackContext"></param>
    private void OnBumperDown(InputAction.CallbackContext callbackContext)
    {
        Debug.Log("Bumper Down");
        _meshingVisualizer.SetRenderers();
        _meshingSubsystemComponent.DestroyAllMeshes();
        _meshingSubsystemComponent.RefreshAllMeshes();
    }

    /// <summary>
    ///  Handles the event for Home down. 
    /// changes from bounded to boundless and viceversa.
    /// </summary>
    /// <param name="callbackContext"></param>
    private void OnMenuDown(InputAction.CallbackContext callbackContext)
    {
        _bounded = !_bounded;
        UpdateBounds();
    }

    /// <summary>
    /// Handle in charge of refreshing all meshes if a new session occurs
    /// </summary>
    /// <param name="inputSubsystem"> The inputSubsystem that invoked this event. </param>
    private void OnTrackingOriginChanged(XRInputSubsystem inputSubsystem)
    {
        _meshingSubsystemComponent.DestroyAllMeshes();
        _meshingSubsystemComponent.RefreshAllMeshes();
    }

    private void UpdateBounds()
    {
        _visualBounds.SetActive(_bounded);
        _meshingSubsystemComponent.gameObject.transform.localScale = _bounded ? _boundedExtentsSize : _boundlessExtentsSize;
    }
}
