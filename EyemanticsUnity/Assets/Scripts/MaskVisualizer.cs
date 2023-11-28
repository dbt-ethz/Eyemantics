using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

public class MaskVisualizer : MonoBehaviour
{


    // Reference to the meshing subsystem
    [SerializeField, Tooltip("The spatial mapper from which to update mesh params.")]
    private MeshingSubsystemComponent meshingSubsystemComponent = null;

    [SerializeField, Space, Tooltip("Size of the bounds extents when bounded setting is disabled.")]
    private Vector3 boundlessExtentsSize = new Vector3(10.0f, 10.0f, 10.0f);

    [SerializeField, Space, Tooltip("Mesh Material")]
    private Material _coloredMaterial = null;

    [SerializeField, Space, Tooltip("Image Capturing and Gaze Class")]
    private ImageGazeInput _gazeInput = null;

    // HashSet to store mesh IDs.
    private HashSet<UnityEngine.XR.MeshId> meshIdSet = new HashSet<UnityEngine.XR.MeshId>();

    private readonly MLPermissions.Callbacks permissionCallbacks = new MLPermissions.Callbacks();

    private Dictionary<int, Color> colorDict = new Dictionary<int, Color>();


    void Awake()
    {
        permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;

        // Ensure meshingSubsystemComponent is assigned in the inspector or find it in the scene.
        if (meshingSubsystemComponent == null)
        {
            meshingSubsystemComponent = FindObjectOfType<MeshingSubsystemComponent>();
        }

        //_gazeInput._camera = Camera.main;
    }



    // Start is called before the first frame update
    void Start()
    {
        meshingSubsystemComponent.meshAdded += OnMeshCreated;
        meshingSubsystemComponent.meshUpdated += OnMeshCreated;
        meshingSubsystemComponent.meshRemoved += OnMeshDestroyed;

        meshingSubsystemComponent.PrefabRenderer.sharedMaterial = _coloredMaterial;

        meshingSubsystemComponent.gameObject.transform.position = new Vector3(0f, 0f, 0f); //_gazeInput._camera.gameObject.transform.position;
        UpdateBounds();

        colorDict.Add(0, new Color(1f, 0f, 0f, 1f)); // Red
        colorDict.Add(1, new Color(0f, 1f, 0f, 1f)); // Green
        colorDict.Add(2, new Color(0f, 0f, 1f, 1f)); // Blue
        colorDict.Add(3, new Color(1f, 1f, 0f, 1f)); // Yellow
        colorDict.Add(4, new Color(0f, 1f, 1f, 1f)); // Cyan
        colorDict.Add(5, new Color(1f, 0f, 1f, 1f)); // Magenta
        colorDict.Add(6, new Color(1f, 0.5f, 0f, 1f)); // Orange
        colorDict.Add(7, new Color(0.5f, 0f, 0.5f, 1f)); // Purple

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDestroy()
    {
        permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;

        meshingSubsystemComponent.meshAdded -= OnMeshCreated;
        meshingSubsystemComponent.meshUpdated -= OnMeshCreated;
        meshingSubsystemComponent.meshRemoved -= OnMeshDestroyed;

    }

    // Function to be called by Meshing Subsystem Component when a new mesh is created.
    public void OnMeshCreated(UnityEngine.XR.MeshId meshId)
    {
        meshIdSet.Add(meshId);

        if (meshingSubsystemComponent.meshIdToGameObjectMap.ContainsKey(meshId))
        {
            MeshRenderer meshRenderer =
                meshingSubsystemComponent.meshIdToGameObjectMap[meshId].GetComponent<MeshRenderer>();
            meshRenderer.enabled = true;

            MeshFilter meshFilter = meshingSubsystemComponent.meshIdToGameObjectMap[meshId].GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                Vector3[] vertices = meshFilter.mesh.vertices;
                Color[] colors = new Color[vertices.Length];
                double heightThreshold = 2f;
                for (int i = 0; i < vertices.Length; i++)
                {
                    // Check the vertex position and assign a color based on your criteria
                    // For example, if the vertex is above a certain height, color it red
                    if (vertices[i].y > heightThreshold)
                    {
                        colors[i] = Color.red;
                        //Debug.Log("Above threshold");
                    }
                    else
                    {
                        colors[i] = Color.blue;
                        //Debug.Log("Below threshold");
                    }
                }

                meshFilter.mesh.colors = colors;
            }

            //Debug.Log(meshingSubsystemComponent.meshIdToGameObjectMap[meshId].GetComponent<MeshFilter>().mesh.colors);

            // Assuming _coloredMaterial is the material you want to modify.
            //Material newMaterial = new Material(_coloredMaterial);
            //newMaterial.color = Color.white; // GetColor(meshCount++); // Set your desired color here.
            //meshRenderer.material = _coloredMaterial;
        }

        //Debug.Log("Mesh Created with ID " + meshId.ToString());
    }



    // Function to be called by Meshing Subsystem Component when a mesh is destroyed.
    public void OnMeshDestroyed(UnityEngine.XR.MeshId meshId)
    {

        meshIdSet.Remove(meshId);
        //Debug.Log("Mesh Removed with ID " + meshId.ToString());
    }

    // Function to get all mesh IDs.
    public UnityEngine.XR.MeshId[] GetAllMeshIDs()
    {
        return System.Linq.Enumerable.ToArray(meshIdSet);
    }

    private void OnPermissionGranted(string permission)
    {
        meshingSubsystemComponent.enabled = true;
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Failed to create Meshing Subsystem due to missing or denied {MLPermission.SpatialMapping} permission. Please add to manifest. Disabling script.");
        enabled = false;
        meshingSubsystemComponent.enabled = false;
    }

    private void UpdateBounds()
    {
        meshingSubsystemComponent.gameObject.transform.localScale = boundlessExtentsSize;
    }

    public Color GetColor(int key)
    {
        key = key % colorDict.Count;
        if (colorDict.TryGetValue(key, out Color color))
        {
            return color;
        }
        else
        {
            Debug.LogWarning("Color key not found, returning default color");
            return Color.white;
        }
    }

    private void colorMeshVertices(UnityEngine.XR.MeshId meshId)
    {
        MeshFilter meshFilter = meshingSubsystemComponent.meshIdToGameObjectMap[meshId].GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            Vector3[] vertices = meshFilter.mesh.vertices;
            Color[] colors = new Color[vertices.Length];
            double heightThreshold = 2f;
            for (int i = 0; i < vertices.Length; i++)
            {
                // Check the vertex position and assign a color based on your criteria
                // For example, if the vertex is above a certain height, color it red
                if (vertices[i].y > heightThreshold)
                {
                    colors[i] = Color.red;
                    //Debug.Log("Above threshold");
                }
                else
                {
                    colors[i] = Color.blue;
                    //Debug.Log("Below threshold");
                }
            }

            meshFilter.mesh.colors = colors;
        }

    }
}
