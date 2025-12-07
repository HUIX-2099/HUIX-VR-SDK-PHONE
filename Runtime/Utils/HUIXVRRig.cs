/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * VR Rig - Complete VR setup prefab component
 */

using UnityEngine;
using HUIX.PhoneVR.Core;

namespace HUIX.PhoneVR
{
    /// <summary>
    /// All-in-one VR rig setup. Add this to an empty GameObject for complete VR functionality.
    /// </summary>
    [AddComponentMenu("HUIX/Phone VR/VR Rig")]
    public class HUIXVRRig : MonoBehaviour
    {
        #region Serialized Fields
        [Header("=== HUIX VR Rig ===")]
        [Space(10)]
        
        [Header("Components")]
        [SerializeField] private bool _createManager = true;
        [SerializeField] private bool _createCamera = true;
        [SerializeField] private bool _createHeadTracker = true;
        [SerializeField] private bool _createInputManager = true;
        
        [Header("Camera Settings")]
        [SerializeField] private float _initialHeight = 1.6f;
        [SerializeField] private Color _backgroundColor = Color.black;
        [SerializeField] private float _nearClip = 0.1f;
        [SerializeField] private float _farClip = 1000f;
        
        [Header("Head Tracking")]
        [SerializeField] private bool _enableHeadTracking = true;
        [SerializeField] private float _trackingSensitivity = 1f;
        
        [Header("Auto Initialize")]
        [SerializeField] private bool _autoSetup = true;
        #endregion

        #region Private Fields
        private HUIXVRManager _manager;
        private HUIXVRCamera _vrCamera;
        private HUIXHeadTracker _headTracker;
        private HUIXInputManager _inputManager;
        private Transform _cameraHolder;
        private Camera _mainCamera;
        #endregion

        #region Properties
        public HUIXVRManager Manager => _manager;
        public HUIXVRCamera VRCamera => _vrCamera;
        public HUIXHeadTracker HeadTracker => _headTracker;
        public HUIXInputManager InputManager => _inputManager;
        public Camera MainCamera => _mainCamera;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_autoSetup)
            {
                Setup();
            }
        }

        private void OnValidate()
        {
            _initialHeight = Mathf.Max(0, _initialHeight);
            _nearClip = Mathf.Max(0.01f, _nearClip);
            _farClip = Mathf.Max(_nearClip + 1, _farClip);
        }
        #endregion

        #region Setup
        /// <summary>
        /// Setup the VR rig with all necessary components
        /// </summary>
        public void Setup()
        {
            Debug.Log("[HUIX VR] Setting up VR Rig...");

            // Create VR Manager
            if (_createManager)
            {
                SetupManager();
            }

            // Create Camera hierarchy
            if (_createCamera)
            {
                SetupCameraHierarchy();
            }

            // Create Head Tracker
            if (_createHeadTracker)
            {
                SetupHeadTracker();
            }

            // Create Input Manager
            if (_createInputManager)
            {
                SetupInputManager();
            }

            Debug.Log("[HUIX VR] VR Rig setup complete!");
        }

        private void SetupManager()
        {
            _manager = GetComponent<HUIXVRManager>();
            if (_manager == null)
            {
                _manager = gameObject.AddComponent<HUIXVRManager>();
            }
        }

        private void SetupCameraHierarchy()
        {
            // Create camera holder (for head tracking rotation)
            _cameraHolder = transform.Find("Camera Holder");
            if (_cameraHolder == null)
            {
                GameObject holderObj = new GameObject("Camera Holder");
                holderObj.transform.SetParent(transform);
                holderObj.transform.localPosition = new Vector3(0, _initialHeight, 0);
                holderObj.transform.localRotation = Quaternion.identity;
                _cameraHolder = holderObj.transform;
            }

            // Find or create main camera
            Transform cameraTransform = _cameraHolder.Find("Main Camera");
            if (cameraTransform == null)
            {
                // Check if Camera.main exists and repurpose it
                if (Camera.main != null && Camera.main.transform.parent == null)
                {
                    Camera.main.transform.SetParent(_cameraHolder);
                    Camera.main.transform.localPosition = Vector3.zero;
                    Camera.main.transform.localRotation = Quaternion.identity;
                    cameraTransform = Camera.main.transform;
                }
                else
                {
                    GameObject cameraObj = new GameObject("Main Camera");
                    cameraObj.transform.SetParent(_cameraHolder);
                    cameraObj.transform.localPosition = Vector3.zero;
                    cameraObj.transform.localRotation = Quaternion.identity;
                    cameraObj.tag = "MainCamera";
                    cameraTransform = cameraObj.transform;
                }
            }

            // Setup camera component
            _mainCamera = cameraTransform.GetComponent<Camera>();
            if (_mainCamera == null)
            {
                _mainCamera = cameraTransform.gameObject.AddComponent<Camera>();
            }

            _mainCamera.backgroundColor = _backgroundColor;
            _mainCamera.nearClipPlane = _nearClip;
            _mainCamera.farClipPlane = _farClip;
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;

            // Add VR Camera component
            _vrCamera = _mainCamera.GetComponent<HUIXVRCamera>();
            if (_vrCamera == null)
            {
                _vrCamera = _mainCamera.gameObject.AddComponent<HUIXVRCamera>();
            }

            // Add audio listener if not present
            if (_mainCamera.GetComponent<AudioListener>() == null)
            {
                _mainCamera.gameObject.AddComponent<AudioListener>();
            }
        }

        private void SetupHeadTracker()
        {
            if (_cameraHolder == null) return;

            _headTracker = _cameraHolder.GetComponent<HUIXHeadTracker>();
            if (_headTracker == null)
            {
                _headTracker = _cameraHolder.gameObject.AddComponent<HUIXHeadTracker>();
            }

            _headTracker.EnableTracking(_enableHeadTracking);
        }

        private void SetupInputManager()
        {
            _inputManager = GetComponent<HUIXInputManager>();
            if (_inputManager == null)
            {
                _inputManager = gameObject.AddComponent<HUIXInputManager>();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the camera height
        /// </summary>
        public void SetHeight(float height)
        {
            _initialHeight = height;
            if (_cameraHolder != null)
            {
                _cameraHolder.localPosition = new Vector3(0, height, 0);
            }
        }

        /// <summary>
        /// Reset the rig position
        /// </summary>
        public void ResetPosition()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            
            if (_headTracker != null)
            {
                _headTracker.Recenter();
            }
        }

        /// <summary>
        /// Move the rig to a position
        /// </summary>
        public void MoveTo(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>
        /// Teleport the rig (with optional fade effect)
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
            
            if (_headTracker != null)
            {
                _headTracker.Recenter();
            }
        }
        #endregion

        #region Editor
        private void OnDrawGizmos()
        {
            // Draw camera position
            Vector3 cameraPos = transform.position + Vector3.up * _initialHeight;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(cameraPos, 0.1f);
            
            // Draw forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(cameraPos, transform.forward * 0.5f);
            
            // Draw up direction
            Gizmos.color = Color.green;
            Gizmos.DrawRay(cameraPos, Vector3.up * 0.3f);
        }
        #endregion
    }
}

