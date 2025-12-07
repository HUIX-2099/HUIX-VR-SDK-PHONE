/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * CardboardVR - ONE SCRIPT VR SOLUTION
 * 
 * HOW TO USE:
 * 1. Attach this script to your Main Camera
 * 2. Press Play
 * 3. Done! You have VR!
 * 
 * Works with: Google Cardboard, Daydream, any phone VR headset
 */

using UnityEngine;

namespace HUIX.PhoneVR
{
    /// <summary>
    /// ONE SCRIPT VR - Attach to camera, get instant Cardboard VR!
    /// </summary>
    [AddComponentMenu("HUIX/Phone VR/Cardboard VR (One Script)")]
    [RequireComponent(typeof(Camera))]
    public class CardboardVR : MonoBehaviour
    {
        #region Settings
        
        [Header("=== HEADSET PRESET ===")]
        [SerializeField] private HeadsetType _headset = HeadsetType.GoogleCardboard;
        
        [Header("=== VR SETTINGS ===")]
        [Tooltip("Distance between eyes in meters (IPD)")]
        [Range(0.05f, 0.08f)]
        [SerializeField] private float _eyeSeparation = 0.064f;
        
        [Tooltip("Field of view per eye")]
        [Range(60f, 120f)]
        [SerializeField] private float _fieldOfView = 100f;
        
        [Header("=== LENS DISTORTION ===")]
        [SerializeField] private bool _enableDistortion = true;
        
        [Tooltip("Barrel distortion strength")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _distortionK1 = 0.22f;
        
        [Tooltip("Secondary distortion")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _distortionK2 = 0.02f;
        
        [Header("=== HEAD TRACKING ===")]
        [SerializeField] private bool _enableHeadTracking = true;
        
        [Tooltip("Tracking smoothing (lower = more responsive)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _trackingSmoothing = 0.1f;
        
        [Header("=== DISPLAY ===")]
        [SerializeField] private bool _showDividerLine = true;
        [SerializeField] private Color _dividerColor = Color.black;
        
        #endregion
        
        #region Headset Presets
        
        public enum HeadsetType
        {
            GoogleCardboard,
            GoogleDaydream,
            SamsungGearVR,
            GenericVR,
            Custom
        }
        
        #endregion
        
        #region Private Fields
        
        private Camera _originalCamera;
        private Camera _leftEye;
        private Camera _rightEye;
        private Transform _head;
        
        private Quaternion _gyroOffset = Quaternion.identity;
        private Quaternion _targetRotation;
        private bool _gyroAvailable;
        
        private Material _distortionMat;
        private RenderTexture _leftRT;
        private RenderTexture _rightRT;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            ApplyHeadsetPreset(_headset);
        }
        
        private void Start()
        {
            SetupVR();
            SetupHeadTracking();
            
            Debug.Log($"[HUIX VR] Cardboard VR Active! Headset: {_headset}");
        }
        
        private void Update()
        {
            UpdateHeadTracking();
            HandleInput();
        }
        
        private void OnDestroy()
        {
            CleanupRenderTextures();
        }
        
        #endregion
        
        #region VR Setup
        
        private void SetupVR()
        {
            _originalCamera = GetComponent<Camera>();
            
            // Create head transform
            _head = new GameObject("VR Head").transform;
            _head.SetParent(transform.parent);
            _head.localPosition = transform.localPosition;
            _head.localRotation = transform.localRotation;
            
            // Move this camera under head
            transform.SetParent(_head);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            
            // Setup stereo cameras
            CreateStereoCameras();
            
            // Setup distortion
            if (_enableDistortion)
            {
                SetupDistortion();
            }
            
            // Disable original camera rendering (we use left/right eyes)
            _originalCamera.cullingMask = 0;
            _originalCamera.clearFlags = CameraClearFlags.Nothing;
            _originalCamera.depth = 100;
        }
        
        private void CreateStereoCameras()
        {
            // LEFT EYE
            GameObject leftObj = new GameObject("Left Eye");
            leftObj.transform.SetParent(transform);
            leftObj.transform.localPosition = new Vector3(-_eyeSeparation / 2f, 0, 0);
            leftObj.transform.localRotation = Quaternion.identity;
            
            _leftEye = leftObj.AddComponent<Camera>();
            CopyCameraSettings(_originalCamera, _leftEye);
            _leftEye.fieldOfView = _fieldOfView;
            _leftEye.rect = new Rect(0, 0, 0.5f, 1);
            _leftEye.depth = 0;
            
            // RIGHT EYE
            GameObject rightObj = new GameObject("Right Eye");
            rightObj.transform.SetParent(transform);
            rightObj.transform.localPosition = new Vector3(_eyeSeparation / 2f, 0, 0);
            rightObj.transform.localRotation = Quaternion.identity;
            
            _rightEye = rightObj.AddComponent<Camera>();
            CopyCameraSettings(_originalCamera, _rightEye);
            _rightEye.fieldOfView = _fieldOfView;
            _rightEye.rect = new Rect(0.5f, 0, 0.5f, 1);
            _rightEye.depth = 0;
            
            // Audio listener on left eye only
            if (GetComponent<AudioListener>() != null)
            {
                Destroy(GetComponent<AudioListener>());
            }
            leftObj.AddComponent<AudioListener>();
        }
        
        private void CopyCameraSettings(Camera from, Camera to)
        {
            to.clearFlags = from.clearFlags;
            to.backgroundColor = from.backgroundColor;
            to.cullingMask = from.cullingMask;
            to.nearClipPlane = from.nearClipPlane;
            to.farClipPlane = from.farClipPlane;
            to.renderingPath = from.renderingPath;
            to.allowHDR = from.allowHDR;
            to.allowMSAA = from.allowMSAA;
        }
        
        private void SetupDistortion()
        {
            int eyeWidth = Screen.width / 2;
            int eyeHeight = Screen.height;
            
            _leftRT = new RenderTexture(eyeWidth, eyeHeight, 24);
            _rightRT = new RenderTexture(eyeWidth, eyeHeight, 24);
            
            _leftEye.targetTexture = _leftRT;
            _rightEye.targetTexture = _rightRT;
            
            // Add renderer component
            VRRenderer renderer = gameObject.AddComponent<VRRenderer>();
            renderer.Setup(_leftRT, _rightRT, _distortionK1, _distortionK2, _showDividerLine, _dividerColor);
        }
        
        private void CleanupRenderTextures()
        {
            if (_leftRT != null) { _leftRT.Release(); Destroy(_leftRT); }
            if (_rightRT != null) { _rightRT.Release(); Destroy(_rightRT); }
        }
        
        #endregion
        
        #region Head Tracking
        
        private void SetupHeadTracking()
        {
            if (!_enableHeadTracking) return;
            
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                _gyroAvailable = true;
                Recenter();
                Debug.Log("[HUIX VR] Gyroscope head tracking enabled");
            }
            else
            {
                Debug.Log("[HUIX VR] No gyroscope - use mouse (right-click + drag)");
            }
            
            _targetRotation = _head.localRotation;
        }
        
        private void UpdateHeadTracking()
        {
            if (!_enableHeadTracking) return;
            
            if (_gyroAvailable)
            {
                // Convert gyroscope to Unity rotation
                Quaternion gyro = Input.gyro.attitude;
                Quaternion rot = new Quaternion(gyro.x, gyro.y, -gyro.z, -gyro.w);
                _targetRotation = _gyroOffset * rot * Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                // Mouse look for testing in editor
                if (Input.GetMouseButton(1))
                {
                    float mx = Input.GetAxis("Mouse X") * 3f;
                    float my = Input.GetAxis("Mouse Y") * 3f;
                    _targetRotation *= Quaternion.Euler(-my, mx, 0);
                }
            }
            
            // Apply smoothing
            if (_trackingSmoothing > 0)
            {
                _head.localRotation = Quaternion.Slerp(_head.localRotation, _targetRotation, 
                    Time.deltaTime / _trackingSmoothing);
            }
            else
            {
                _head.localRotation = _targetRotation;
            }
        }
        
        #endregion
        
        #region Input
        
        private void HandleInput()
        {
            // Double tap to recenter
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.tapCount == 2 && touch.phase == TouchPhase.Ended)
                {
                    Recenter();
                }
            }
            
            // R key to recenter (testing)
            if (Input.GetKeyDown(KeyCode.R))
            {
                Recenter();
            }
            
            // Space to toggle VR
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ToggleVR();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Recenter the view (make current direction = forward)
        /// </summary>
        public void Recenter()
        {
            if (_gyroAvailable)
            {
                Quaternion gyro = Input.gyro.attitude;
                Quaternion currentRot = new Quaternion(gyro.x, gyro.y, -gyro.z, -gyro.w);
                _gyroOffset = Quaternion.Inverse(currentRot * Quaternion.Euler(90f, 0f, 0f));
            }
            else
            {
                _targetRotation = Quaternion.identity;
            }
            Debug.Log("[HUIX VR] View recentered");
        }
        
        /// <summary>
        /// Toggle VR mode on/off
        /// </summary>
        public void ToggleVR()
        {
            bool vrActive = _leftEye.gameObject.activeSelf;
            _leftEye.gameObject.SetActive(!vrActive);
            _rightEye.gameObject.SetActive(!vrActive);
            
            if (vrActive)
            {
                // Switch to normal view
                _originalCamera.cullingMask = -1;
                _originalCamera.clearFlags = CameraClearFlags.Skybox;
                _originalCamera.rect = new Rect(0, 0, 1, 1);
            }
            else
            {
                // Switch to VR view
                _originalCamera.cullingMask = 0;
                _originalCamera.clearFlags = CameraClearFlags.Nothing;
            }
            
            Debug.Log($"[HUIX VR] VR Mode: {!vrActive}");
        }
        
        /// <summary>
        /// Set IPD (eye separation)
        /// </summary>
        public void SetIPD(float ipd)
        {
            _eyeSeparation = ipd;
            if (_leftEye != null)
                _leftEye.transform.localPosition = new Vector3(-ipd / 2f, 0, 0);
            if (_rightEye != null)
                _rightEye.transform.localPosition = new Vector3(ipd / 2f, 0, 0);
        }
        
        /// <summary>
        /// Apply a headset preset
        /// </summary>
        public void SetHeadset(HeadsetType type)
        {
            _headset = type;
            ApplyHeadsetPreset(type);
        }
        
        #endregion
        
        #region Headset Presets
        
        private void ApplyHeadsetPreset(HeadsetType type)
        {
            switch (type)
            {
                case HeadsetType.GoogleCardboard:
                    _eyeSeparation = 0.064f;
                    _fieldOfView = 100f;
                    _distortionK1 = 0.22f;
                    _distortionK2 = 0.02f;
                    break;
                    
                case HeadsetType.GoogleDaydream:
                    _eyeSeparation = 0.064f;
                    _fieldOfView = 96f;
                    _distortionK1 = 0.18f;
                    _distortionK2 = 0.02f;
                    break;
                    
                case HeadsetType.SamsungGearVR:
                    _eyeSeparation = 0.064f;
                    _fieldOfView = 96f;
                    _distortionK1 = 0.215f;
                    _distortionK2 = 0.025f;
                    break;
                    
                case HeadsetType.GenericVR:
                    _eyeSeparation = 0.064f;
                    _fieldOfView = 90f;
                    _distortionK1 = 0.2f;
                    _distortionK2 = 0.02f;
                    break;
                    
                case HeadsetType.Custom:
                    // Keep current values
                    break;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Renders stereo view with lens distortion
    /// </summary>
    public class VRRenderer : MonoBehaviour
    {
        private RenderTexture _leftTex;
        private RenderTexture _rightTex;
        private Material _material;
        private bool _showDivider;
        private Color _dividerColor;
        private float _k1, _k2;
        
        public void Setup(RenderTexture left, RenderTexture right, float k1, float k2, bool divider, Color divColor)
        {
            _leftTex = left;
            _rightTex = right;
            _k1 = k1;
            _k2 = k2;
            _showDivider = divider;
            _dividerColor = divColor;
            
            // Create distortion material
            _material = new Material(Shader.Find("Unlit/Texture"));
        }
        
        private void OnPostRender()
        {
            if (_leftTex == null || _rightTex == null) return;
            
            GL.PushMatrix();
            GL.LoadOrtho();
            
            // Draw left eye with distortion
            DrawEyeWithDistortion(_leftTex, 0f, 0.5f, true);
            
            // Draw right eye with distortion  
            DrawEyeWithDistortion(_rightTex, 0.5f, 0.5f, false);
            
            // Draw center divider line
            if (_showDivider)
            {
                DrawDivider();
            }
            
            GL.PopMatrix();
        }
        
        private void DrawEyeWithDistortion(RenderTexture tex, float xOffset, float width, bool isLeft)
        {
            _material.mainTexture = tex;
            _material.SetPass(0);
            
            // Draw with barrel distortion using multiple quads
            int segments = 20;
            float segWidth = width / segments;
            float segHeight = 1f / segments;
            
            for (int y = 0; y < segments; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    // Calculate UV coordinates with distortion
                    float u0 = (float)x / segments;
                    float v0 = (float)y / segments;
                    float u1 = (float)(x + 1) / segments;
                    float v1 = (float)(y + 1) / segments;
                    
                    // Apply barrel distortion to screen position
                    Vector2 p00 = ApplyDistortion(new Vector2(u0, v0));
                    Vector2 p10 = ApplyDistortion(new Vector2(u1, v0));
                    Vector2 p11 = ApplyDistortion(new Vector2(u1, v1));
                    Vector2 p01 = ApplyDistortion(new Vector2(u0, v1));
                    
                    // Scale to eye region
                    p00.x = xOffset + p00.x * width;
                    p10.x = xOffset + p10.x * width;
                    p11.x = xOffset + p11.x * width;
                    p01.x = xOffset + p01.x * width;
                    
                    GL.Begin(GL.QUADS);
                    GL.TexCoord2(u0, v0); GL.Vertex3(p00.x, p00.y, 0);
                    GL.TexCoord2(u1, v0); GL.Vertex3(p10.x, p10.y, 0);
                    GL.TexCoord2(u1, v1); GL.Vertex3(p11.x, p11.y, 0);
                    GL.TexCoord2(u0, v1); GL.Vertex3(p01.x, p01.y, 0);
                    GL.End();
                }
            }
        }
        
        private Vector2 ApplyDistortion(Vector2 uv)
        {
            // Convert to centered coordinates (-0.5 to 0.5)
            Vector2 centered = uv - new Vector2(0.5f, 0.5f);
            
            // Calculate distance from center
            float r2 = centered.x * centered.x + centered.y * centered.y;
            float r4 = r2 * r2;
            
            // Apply barrel distortion formula
            float distortion = 1f + _k1 * r2 + _k2 * r4;
            
            // Apply distortion
            Vector2 result = centered * distortion + new Vector2(0.5f, 0.5f);
            
            return result;
        }
        
        private void DrawDivider()
        {
            GL.Begin(GL.QUADS);
            GL.Color(_dividerColor);
            float lineWidth = 0.003f;
            GL.Vertex3(0.5f - lineWidth, 0, 0);
            GL.Vertex3(0.5f + lineWidth, 0, 0);
            GL.Vertex3(0.5f + lineWidth, 1, 0);
            GL.Vertex3(0.5f - lineWidth, 1, 0);
            GL.End();
            GL.Color(Color.white);
        }
    }
}

