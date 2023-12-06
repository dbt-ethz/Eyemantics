// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2019-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using System;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace MagicLeap.Examples
{
    /// <summary>
    /// This class allows you to change meshing properties at runtime, including the rendering mode.
    /// Manages the MeshingSubsystemComponent behaviour and tracks the meshes.
    /// </summary>
    public class MeshingVisualizer : MonoBehaviour
    {
        public enum RenderMode
        {
            None,
            Wireframe,
            Colored,
            PointCloud,
        }

        

        [SerializeField, Tooltip("The MeshingSubsystemComponent from which to get update on mesh types.")]
        private MeshingSubsystemComponent _meshingSubsystemComponent = null;

        [SerializeField, Tooltip("The material to apply for occlusion.")]
        private Material _occlusionMaterial = null;

        [SerializeField, Tooltip("The material to apply for wireframe rendering.")]
        private Material _wireframeMaterial = null;

        [SerializeField, Tooltip("The material to apply for colored rendering.")]
        private Material _coloredMaterial = null;

        [SerializeField, Tooltip("The material to apply for point cloud rendering.")]
        private Material _pointCloudMaterial = null;

        [SerializeField, Tooltip("Class to capture image and gaze point.")]
        private ImageGazeInput _imageGazeInput = null;

        [SerializeField, Tooltip("Debug Text Box.")]
        private Text debugTextBox = null;

        // HashSet to store mesh IDs.
        private HashSet<UnityEngine.XR.MeshId> meshIdSet = new HashSet<UnityEngine.XR.MeshId>();

        public RenderMode renderMode
        {
            get; private set;
        } = RenderMode.Wireframe;

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
            if (_occlusionMaterial == null)
            {
                Debug.LogError("Error: MeshingVisualizer._occlusionMaterial is not set, disabling script!");
                enabled = false;
                return;
            }
            if (_wireframeMaterial == null)
            {
                Debug.LogError("Error: MeshingVisualizer._wireframeMaterial is not set, disabling script!");
                enabled = false;
                return;
            }
            if (_coloredMaterial == null)
            {
                Debug.LogError("Error: MeshingVisualizer._coloredMaterial is not set, disabling script!");
                enabled = false;
                return;
            }
            if (_pointCloudMaterial == null)
            {
                Debug.LogError("Error: MeshingVisualizer._pointCloudMaterial is not set, disabling script!");
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

        /// <summary>
        /// Set the render material on the meshes.
        /// </summary>
        /// <param name="mode">The render mode that should be used on the material.</param>
        public void SetRenderers(RenderMode mode)
        {
            if (renderMode != mode)
            {
                // Set the render mode.
                renderMode = mode;

                _meshingSubsystemComponent.requestedMeshType = (renderMode == RenderMode.PointCloud) ? 
                    MeshingSubsystemComponent.MeshType.PointCloud : 
                    MeshingSubsystemComponent.MeshType.Triangles;

                switch (renderMode)
                {
                    case RenderMode.None:
                        break;
                    case RenderMode.Wireframe:
                        _meshingSubsystemComponent.PrefabRenderer.sharedMaterial = _wireframeMaterial;
                        break;
                    case RenderMode.Colored:
                        _meshingSubsystemComponent.PrefabRenderer.sharedMaterial = _coloredMaterial;
                        break;
                    case RenderMode.PointCloud:
                        _meshingSubsystemComponent.PrefabRenderer.sharedMaterial = _pointCloudMaterial;
                        break;
                    //case RenderMode.Occlusion:
                    //    _meshingSubsystemComponent.PrefabRenderer.sharedMaterial = _occlusionMaterial;
                    //    break;
                    default:
                        throw new ArgumentOutOfRangeException($"unknown renderMode value: {renderMode}");
                }

                _meshingSubsystemComponent.DestroyAllMeshes();
                _meshingSubsystemComponent.RefreshAllMeshes();
            }
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
                    mr.enabled = renderMode != RenderMode.None;
                }

                Vector2 errVec = new Vector2(-1f, -1f);

                if (renderMode == RenderMode.Colored)
                {
                    double heightThreshold = 0f;
                    var meshFilter = meshGameObject.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        Vector3[] vertices = meshFilter.mesh.vertices;
                        Color[] colors = new Color[vertices.Length];

                        Vector2 PrincipalPoint = new Vector2(_imageGazeInput.cameraIntrinsics.PrincipalPoint.x, _imageGazeInput.cameraIntrinsics.PrincipalPoint.x);
                        Vector2 zeroVec = new Vector2(0f, 0f);

                        Color red = new Color(1f, 0f, 0f, 0f);
                        Color blue = new Color(0f, 0f, 1f, 1f);

                        Vector2 pixelLocationRangeX = new Vector2(-50f, 50f);
                        Vector2 pixelLocationRangeY = new Vector2(-50f, 50f);

                        //Debug.Log($"Pixel Location X Range: {pixelLocationRangeX}");
                        //Debug.Log($"Pixel Location Y Range: {pixelLocationRangeY}");



                        for (int i = 0; i < vertices.Length; i++)
                        {
                            // Initialize Color
                            colors[i] = blue;

                            Vector2 pixelLocation = _imageGazeInput.ViewportPointFromWorld(_imageGazeInput.cameraIntrinsics, vertices[i], _imageGazeInput.cameraPos.position, _imageGazeInput.cameraPos.rotation);

                            if (pixelLocation != errVec)
                            {
                                pixelLocation -= PrincipalPoint;
                                //pixelLocation -= _imageGazeInput.cameraIntrinsics.PrincipalPoint;
                                // Check if pixelLocation is within the specified range
                                if (pixelLocation.x >= pixelLocationRangeX.x && pixelLocation.x <= pixelLocationRangeX.y &&
                                    pixelLocation.y >= pixelLocationRangeY.x && pixelLocation.y <= pixelLocationRangeY.y)
                                {
                                    //Debug.Log(pixelLocation);
                                    // Add vertex to filteredVertices and corresponding color to filteredColors
                                    colors[i] = red;
                                }
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
            //Debug.Log("Mesh Removed with ID " + meshId.ToString());
        }
    }
}
