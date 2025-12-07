/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * VR Camera - Stereoscopic rendering system
 */

using UnityEngine;
using HUIX.PhoneVR.Core;

namespace HUIX.PhoneVR
{
    /// <summary>
    /// Handles stereoscopic rendering for phone VR.
    /// Attach this to your main camera to enable VR view.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("HUIX/Phone VR/VR Camera")]
    public class HUIXVRCamera : MonoBehaviour
    {
        #region Serialized Fields
        [Header("=== HUIX VR Camera ===")]
        [Space(10)]
        
        [Header("Stereo Settings")]
        [SerializeField] private bool _stereoEnabled = true;
        [SerializeField] private float _eyeSeparation = 0.064f;
        [SerializeField] private float _convergenceDistance = 10f;
        
        [Header("Rendering")]
        [SerializeField] private LayerMask _cullingMask = -1;
        [SerializeField] private bool _useDistortionCorrection = true;
        [SerializeField] private Material _distortionMaterial;
        
        [Header("References")]
        [SerializeField] private Camera _leftEyeCamera;
        [SerializeField] private Camera _rightEyeCamera;
        [SerializeField] private Transform _leftEyeAnchor;
        [SerializeField] private Transform _rightEyeAnchor;
        #endregion

        #region Private Fields
        private Camera _mainCamera;
        private RenderTexture _leftEyeTexture;
        private RenderTexture _rightEyeTexture;
        private HeadsetProfile _currentProfile;
        private bool _isInitialized;
        #endregion

        #region Properties
        public bool IsStereoEnabled => _stereoEnabled;
        public Camera LeftEyeCamera => _leftEyeCamera;
        public Camera RightEyeCamera => _rightEyeCamera;
        public float EyeSeparation
        {
            get => _eyeSeparation;
            set
            {
                _eyeSeparation = Mathf.Clamp(value, 0.05f, 0.08f);
                UpdateEyePositions();
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _mainCamera = GetComponent<Camera>();
            Initialize();
        }

        private void OnEnable()
        {
            if (_stereoEnabled && _isInitialized)
            {
                EnableStereoRendering(true);
            }
        }

        private void OnDisable()
        {
            EnableStereoRendering(false);
        }

        private void LateUpdate()
        {
            if (_stereoEnabled && _isInitialized)
            {
                UpdateEyeCameras();
            }
        }

        private void OnDestroy()
        {
            CleanupRenderTextures();
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            if (_isInitialized) return;

            // Create eye anchors if not assigned
            if (_leftEyeAnchor == null)
            {
                GameObject leftAnchor = new GameObject("Left Eye Anchor");
                leftAnchor.transform.SetParent(transform);
                _leftEyeAnchor = leftAnchor.transform;
            }

            if (_rightEyeAnchor == null)
            {
                GameObject rightAnchor = new GameObject("Right Eye Anchor");
                rightAnchor.transform.SetParent(transform);
                _rightEyeAnchor = rightAnchor.transform;
            }

            // Create eye cameras if not assigned
            if (_leftEyeCamera == null)
            {
                _leftEyeCamera = CreateEyeCamera("Left Eye Camera", _leftEyeAnchor);
            }

            if (_rightEyeCamera == null)
            {
                _rightEyeCamera = CreateEyeCamera("Right Eye Camera", _rightEyeAnchor);
            }

            // Load distortion material
            if (_distortionMaterial == null)
            {
                _distortionMaterial = Resources.Load<Material>("HUIX/LensDistortion");
            }

            // Get headset profile from manager
            if (HUIXVRManager.Instance != null && HUIXVRManager.Instance.CurrentHeadset != null)
            {
                ApplyHeadsetProfile(HUIXVRManager.Instance.CurrentHeadset);
            }

            UpdateEyePositions();
            _isInitialized = true;

            Debug.Log("[HUIX VR] VR Camera initialized");
        }

        private Camera CreateEyeCamera(string cameraName, Transform parent)
        {
            GameObject cameraObj = new GameObject(cameraName);
            cameraObj.transform.SetParent(parent);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.transform.localRotation = Quaternion.identity;

            Camera cam = cameraObj.AddComponent<Camera>();
            cam.CopyFrom(_mainCamera);
            cam.enabled = false; // Will be manually rendered
            
            return cam;
        }
        #endregion

        #region Stereo Rendering
        /// <summary>
        /// Enable or disable stereoscopic rendering
        /// </summary>
        public void EnableStereoRendering(bool enable)
        {
            _stereoEnabled = enable;

            if (enable)
            {
                SetupStereoRendering();
                _mainCamera.enabled = false;
            }
            else
            {
                _mainCamera.enabled = true;
                _mainCamera.rect = new Rect(0, 0, 1, 1);
                
                if (_leftEyeCamera != null) _leftEyeCamera.enabled = false;
                if (_rightEyeCamera != null) _rightEyeCamera.enabled = false;
            }
        }

        private void SetupStereoRendering()
        {
            CreateRenderTextures();

            // Configure eye cameras
            if (_leftEyeCamera != null)
            {
                _leftEyeCamera.enabled = true;
                _leftEyeCamera.rect = new Rect(0, 0, 0.5f, 1);
                _leftEyeCamera.targetTexture = _useDistortionCorrection ? _leftEyeTexture : null;
                _leftEyeCamera.cullingMask = _cullingMask;
            }

            if (_rightEyeCamera != null)
            {
                _rightEyeCamera.enabled = true;
                _rightEyeCamera.rect = new Rect(0.5f, 0, 0.5f, 1);
                _rightEyeCamera.targetTexture = _useDistortionCorrection ? _rightEyeTexture : null;
                _rightEyeCamera.cullingMask = _cullingMask;
            }

            UpdateEyePositions();
        }

        private void CreateRenderTextures()
        {
            int width = Screen.width / 2;
            int height = Screen.height;

            float renderScale = 1f;
            if (HUIXVRManager.Instance != null)
            {
                renderScale = HUIXVRManager.Instance.RenderScale;
            }

            width = Mathf.RoundToInt(width * renderScale);
            height = Mathf.RoundToInt(height * renderScale);

            if (_leftEyeTexture == null || _leftEyeTexture.width != width)
            {
                CleanupRenderTextures();

                _leftEyeTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                _leftEyeTexture.antiAliasing = 2;
                _leftEyeTexture.name = "HUIX_LeftEye";

                _rightEyeTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                _rightEyeTexture.antiAliasing = 2;
                _rightEyeTexture.name = "HUIX_RightEye";
            }
        }

        private void CleanupRenderTextures()
        {
            if (_leftEyeTexture != null)
            {
                _leftEyeTexture.Release();
                DestroyImmediate(_leftEyeTexture);
            }

            if (_rightEyeTexture != null)
            {
                _rightEyeTexture.Release();
                DestroyImmediate(_rightEyeTexture);
            }
        }

        private void UpdateEyePositions()
        {
            float halfSeparation = _eyeSeparation / 2f;

            if (_leftEyeAnchor != null)
            {
                _leftEyeAnchor.localPosition = new Vector3(-halfSeparation, 0, 0);
            }

            if (_rightEyeAnchor != null)
            {
                _rightEyeAnchor.localPosition = new Vector3(halfSeparation, 0, 0);
            }
        }

        private void UpdateEyeCameras()
        {
            // Sync camera properties
            if (_leftEyeCamera != null)
            {
                _leftEyeCamera.fieldOfView = _mainCamera.fieldOfView;
                _leftEyeCamera.nearClipPlane = _mainCamera.nearClipPlane;
                _leftEyeCamera.farClipPlane = _mainCamera.farClipPlane;
                _leftEyeCamera.clearFlags = _mainCamera.clearFlags;
                _leftEyeCamera.backgroundColor = _mainCamera.backgroundColor;
            }

            if (_rightEyeCamera != null)
            {
                _rightEyeCamera.fieldOfView = _mainCamera.fieldOfView;
                _rightEyeCamera.nearClipPlane = _mainCamera.nearClipPlane;
                _rightEyeCamera.farClipPlane = _mainCamera.farClipPlane;
                _rightEyeCamera.clearFlags = _mainCamera.clearFlags;
                _rightEyeCamera.backgroundColor = _mainCamera.backgroundColor;
            }
        }
        #endregion

        #region Post Processing
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!_stereoEnabled || !_useDistortionCorrection || _distortionMaterial == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            // Apply lens distortion correction
            if (_currentProfile != null)
            {
                _distortionMaterial.SetFloat("_DistortionK1", _currentProfile.DistortionK1);
                _distortionMaterial.SetFloat("_DistortionK2", _currentProfile.DistortionK2);
                _distortionMaterial.SetFloat("_ChromaticRed", _currentProfile.ChromaticRed);
                _distortionMaterial.SetFloat("_ChromaticGreen", _currentProfile.ChromaticGreen);
                _distortionMaterial.SetFloat("_ChromaticBlue", _currentProfile.ChromaticBlue);
            }

            // Render left eye
            _distortionMaterial.SetTexture("_MainTex", _leftEyeTexture);
            _distortionMaterial.SetFloat("_EyeOffset", -0.25f);
            Graphics.Blit(_leftEyeTexture, destination, _distortionMaterial, 0);

            // Render right eye
            _distortionMaterial.SetTexture("_MainTex", _rightEyeTexture);
            _distortionMaterial.SetFloat("_EyeOffset", 0.25f);
            Graphics.Blit(_rightEyeTexture, destination, _distortionMaterial, 0);
        }
        #endregion

        #region Profile Application
        /// <summary>
        /// Apply a headset profile to configure the cameras
        /// </summary>
        public void ApplyHeadsetProfile(HeadsetProfile profile)
        {
            if (profile == null) return;

            _currentProfile = profile;
            _eyeSeparation = profile.GetEyeSeparation();
            
            // Apply FOV
            if (_mainCamera != null)
            {
                _mainCamera.fieldOfView = profile.FieldOfView;
            }

            UpdateEyePositions();

            Debug.Log($"[HUIX VR] Applied headset profile: {profile.ProfileName}");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the world position being looked at (center of view)
        /// </summary>
        public Vector3 GetGazePoint(float maxDistance = 100f)
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                return hit.point;
            }
            return transform.position + transform.forward * maxDistance;
        }

        /// <summary>
        /// Get the gaze ray from the center of the VR view
        /// </summary>
        public Ray GetGazeRay()
        {
            return new Ray(transform.position, transform.forward);
        }
        #endregion
    }
}

