using System;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;


/// <summary>
/// This class allows you to change meshing properties at runtime, including the rendering mode.
/// Manages the MeshingSubsystemComponent behaviour and tracks the meshes.
/// </summary>
public class MeshingVisualizer : MonoBehaviour
{



    [SerializeField, Tooltip("The MeshingSubsystemComponent from which to get update on mesh types.")]
    private MeshingSubsystemComponent _meshingSubsystemComponent = null;

    [SerializeField, Tooltip("The material to apply for colored rendering.")]
    private Material _coloredMaterial = null;

    [SerializeField, Tooltip("Class to capture image and gaze point.")]
    private ImageGazeInput _imageGazeInput = null;

    // HashSet to store mesh IDs.
    private HashSet<UnityEngine.XR.MeshId> meshIdSet = new HashSet<UnityEngine.XR.MeshId>();

    public bool renderMask = true;

    private Vector3 cameraPos;
    private Quaternion cameraRot;

    /// <summary>
    /// Start listening for MeshingSubsystemComponent events.
    /// </summary>
    void Awake()
    {
            
        // Validate all required game objects.
        if (_meshingSubsystemComponent == null)
        {
            Debug.LogError("Error: MeshingVisualizer._meshingSubsystemComponent is not set, disabling script!");
            enabled = false;
            return;
        }
        if (_coloredMaterial == null)
        {
            Debug.LogError("Error: MeshingVisualizer._coloredMaterial is not set, disabling script!");
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// Register for new and updated fragments.
    /// </summary>
    void Start()
    {
        _meshingSubsystemComponent.meshAdded += HandleOnMeshReady;
        _meshingSubsystemComponent.meshUpdated += HandleOnMeshReady;
        _meshingSubsystemComponent.meshRemoved += OnMeshDestroyed;
    }

    /// <summary>
    /// Unregister callbacks.
    /// </summary>
    void OnDestroy()
    {
        _meshingSubsystemComponent.meshAdded -= HandleOnMeshReady;
        _meshingSubsystemComponent.meshUpdated -= HandleOnMeshReady;
        _meshingSubsystemComponent.meshRemoved -= OnMeshDestroyed;
    }

    void Update()
    {

        if (TCPServer.newMaskFlag)
        {
            cameraPos = _imageGazeInput.cameraPos.position;
            cameraRot = _imageGazeInput.cameraPos.rotation;
            UpdateAllMeshes();

            TCPServer.newMaskFlag = false;

        }

    }

    /// <summary>
    /// Set the render material on the meshes.
    /// </summary>
    /// <param name="mode">The render mode that should be used on the material.</param>
    public void SetRenderers()
    {

        renderMask = !renderMask;
        _meshingSubsystemComponent.DestroyAllMeshes();
        _meshingSubsystemComponent.RefreshAllMeshes();
        _meshingSubsystemComponent.PrefabRenderer.sharedMaterial = _coloredMaterial;
    }
        
    /// <summary>
    /// Handles the MeshReady event, which tracks and assigns the correct mesh renderer materials.
    /// </summary>
    /// <param name="meshId">Id of the mesh that got added / upated.</param>
    private void HandleOnMeshReady(UnityEngine.XR.MeshId meshId)
    {

        meshIdSet.Add(meshId);
        if (_meshingSubsystemComponent.meshIdToGameObjectMap.TryGetValue(meshId, out var meshGameObject))
        {
            var mr = meshGameObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = renderMask;
            }

            UpdateMesh(meshId);
                
        }
    }

    public void UpdateAllMeshes()
    {

        foreach (UnityEngine.XR.MeshId meshId in meshIdSet)
        {
            UpdateMesh(meshId);
        }

    }

    public void UpdateMesh(UnityEngine.XR.MeshId meshId)
    {

        if (_meshingSubsystemComponent.meshIdToGameObjectMap.TryGetValue(meshId, out var meshGameObject))
        {
            var mr = meshGameObject.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = renderMask;
            }

            Vector2 errVec = new Vector2(-1f, -1f);

            if (renderMask)
            {
                var meshFilter = meshGameObject.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    Vector3[] vertices = meshFilter.mesh.vertices;
                    Color[] colors = new Color[vertices.Length];

                    Color red = new Color(1f, 0f, 0f, 1f);
                    Color transparent = new Color(1f, 1f, 1f, 0f);

                    if (!(TCPServer.mask == null || TCPServer.mask.Length == 0))
                    {

                        for (int i = 0; i < vertices.Length; i++)
                        {
                            Quaternion additionalRotation = Quaternion.Euler(0f, 0f, 0f);

                            Vector2 pixelLocation = _imageGazeInput.ViewportPointFromWorld(_imageGazeInput.cameraIntrinsics, vertices[i], cameraPos, cameraRot, additionalRotation);

                            if (pixelLocation != errVec)
                            {

                                int xIndex = Mathf.RoundToInt(pixelLocation.y);
                                int yIndex = Mathf.RoundToInt(pixelLocation.x);

                                //// Check if the indices are within bounds before accessing the element
                                if (xIndex >= 0 && xIndex < TCPServer.mask.Length && yIndex >= 0 && yIndex < TCPServer.mask[0].Length)
                                {
                                    if (TCPServer.mask[xIndex][yIndex])
                                    {
                                        colors[i] = red;
                                    }

                                }
                            }
                        }

                    } else
                    {
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            colors[i] = transparent;
                        }
                    }

                    meshFilter.mesh.colors = colors;
                }
            }
        }


    }

    // Function to be called by Meshing Subsystem Component when a mesh is destroyed.
    public void OnMeshDestroyed(UnityEngine.XR.MeshId meshId)
    {
        meshIdSet.Remove(meshId);
    }
}

