/*
 * HUIX-VR-SDK-PHONE
 * Simple Phone VR SDK - Just attach to camera and GO!
 * 
 * Based on Google Cardboard SDK lens distortion parameters
 * Compatible with all Cardboard-style VR viewers
 */

using UnityEngine;

namespace HUIX.VR
{
    /// <summary>
    /// HUIX VR Camera - Attach this single script to your Main Camera.
    /// That's it. You now have VR.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class HUIXVRCamera : MonoBehaviour
    {
        #region Public Settings
        
        [Header("=== HUIX VR SDK ===")]
        [Tooltip("Enable/disable VR mode")]
        public bool vrEnabled = true;
        
        [Header("Device Profile")]
        [Tooltip("Inter-pupillary distance in meters")]
        [Range(0.055f, 0.075f)]
        public float ipd = 0.064f;
        
        [Tooltip("Screen to lens distance in meters")]
        [Range(0.025f, 0.06f)]
        public float screenToLens = 0.042f;
        
        [Header("Lens Distortion")]
        [Tooltip("Enable barrel distortion correction")]
        public bool enableDistortion = true;
        
        [Tooltip("Distortion coefficient K1")]
        [Range(0f, 1f)]
        public float distortionK1 = 0.441f;
        
        [Tooltip("Distortion coefficient K2")]
        [Range(0f, 1f)]
        public float distortionK2 = 0.156f;
        
        [Header("Field of View")]
        [Range(60f, 120f)]
        public float fieldOfView = 95f;
        
        [Header("Head Tracking")]
        [Tooltip("Enable gyroscope head tracking")]
        public bool enableHeadTracking = true;
        
        [Tooltip("Smoothing factor for head movement (0 = instant, 1 = very smooth)")]
        [Range(0f, 0.95f)]
        public float trackingSmoothing = 0.1f;
        
        [Header("Input")]
        [Tooltip("Tap screen to recenter view")]
        public bool tapToRecenter = true;
        
        [Header("Display")]
        [Tooltip("Show center divider line")]
        public bool showDivider = true;
        
        [Tooltip("Divider line color")]
        public Color dividerColor = Color.black;
        
        #endregion
        
        #region Private Variables
        
        private Camera mainCamera;
        private Camera leftEyeCamera;
        private Camera rightEyeCamera;
        
        private RenderTexture leftEyeTexture;
        private RenderTexture rightEyeTexture;
        
        private Material distortionMaterial;
        private Material blitMaterial;
        
        private Quaternion gyroRotation = Quaternion.identity;
        private Quaternion targetRotation = Quaternion.identity;
        private Quaternion initialRotation;
        private Quaternion gyroOffset = Quaternion.identity;
        
        private bool gyroSupported;
        private bool initialized;
        private int lastScreenWidth;
        private int lastScreenHeight;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            mainCamera = GetComponent<Camera>();
            Initialize();
        }
        
        private void Start()
        {
            SetupGyroscope();
            
            // Force landscape orientation for VR
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        
        private void Update()
        {
            if (!vrEnabled || !initialized) return;
            
            // Handle input
            HandleInput();
            
            // Head tracking
            if (enableHeadTracking && gyroSupported)
            {
                UpdateHeadTracking();
            }
            
            // Check for screen size changes
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                RecreateRenderTextures();
            }
        }
        
        private void LateUpdate()
        {
            if (!vrEnabled || !initialized) return;
            UpdateEyeCameras();
        }
        
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (!vrEnabled || !initialized || leftEyeTexture == null || rightEyeTexture == null)
            {
                Graphics.Blit(source, destination);
                return;
            }
            
            RenderStereo(destination);
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }
        
        private void OnApplicationPause(bool paused)
        {
            if (gyroSupported)
            {
                Input.gyro.enabled = !paused;
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void Initialize()
        {
            if (initialized) return;
            
            CreateMaterials();
            CreateEyeCameras();
            CreateRenderTextures();
            
            // Main camera only does post-processing, doesn't render scene
            mainCamera.cullingMask = 0;
            mainCamera.clearFlags = CameraClearFlags.Nothing;
            
            initialized = true;
            
            Debug.Log("[HUIX VR] Initialized - VR Mode Active");
        }
        
        private void CreateMaterials()
        {
            // Try to find the distortion shader
            Shader distortionShader = Shader.Find("HUIX/VR/LensDistortion");
            
            if (distortionShader != null)
            {
                distortionMaterial = new Material(distortionShader);
            }
            else
            {
                Debug.LogWarning("[HUIX VR] LensDistortion shader not found. Distortion disabled.");
                enableDistortion = false;
            }
            
            // Simple blit shader for non-distorted rendering
            blitMaterial = new Material(Shader.Find("Unlit/Texture"));
        }
        
        private void CreateEyeCameras()
        {
            // Store original camera settings
            int originalCullingMask = mainCamera.cullingMask == 0 ? -1 : mainCamera.cullingMask;
            CameraClearFlags originalClearFlags = mainCamera.clearFlags;
            Color originalBackgroundColor = mainCamera.backgroundColor;
            float originalNearClip = mainCamera.nearClipPlane;
            float originalFarClip = mainCamera.farClipPlane;
            
            // Create left eye camera
            GameObject leftEyeObj = new GameObject("HUIX_LeftEye");
            leftEyeObj.transform.SetParent(transform, false);
            leftEyeObj.hideFlags = HideFlags.HideAndDontSave;
            leftEyeCamera = leftEyeObj.AddComponent<Camera>();
            leftEyeCamera.CopyFrom(mainCamera);
            leftEyeCamera.cullingMask = originalCullingMask;
            leftEyeCamera.clearFlags = originalClearFlags;
            leftEyeCamera.backgroundColor = originalBackgroundColor;
            leftEyeCamera.nearClipPlane = originalNearClip;
            leftEyeCamera.farClipPlane = originalFarClip;
            leftEyeCamera.fieldOfView = fieldOfView;
            leftEyeCamera.transform.localPosition = new Vector3(-ipd / 2f, 0, 0);
            leftEyeCamera.depth = mainCamera.depth - 2;
            
            // Create right eye camera
            GameObject rightEyeObj = new GameObject("HUIX_RightEye");
            rightEyeObj.transform.SetParent(transform, false);
            rightEyeObj.hideFlags = HideFlags.HideAndDontSave;
            rightEyeCamera = rightEyeObj.AddComponent<Camera>();
            rightEyeCamera.CopyFrom(mainCamera);
            rightEyeCamera.cullingMask = originalCullingMask;
            rightEyeCamera.clearFlags = originalClearFlags;
            rightEyeCamera.backgroundColor = originalBackgroundColor;
            rightEyeCamera.nearClipPlane = originalNearClip;
            rightEyeCamera.farClipPlane = originalFarClip;
            rightEyeCamera.fieldOfView = fieldOfView;
            rightEyeCamera.transform.localPosition = new Vector3(ipd / 2f, 0, 0);
            rightEyeCamera.depth = mainCamera.depth - 1;
        }
        
        private void CreateRenderTextures()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            
            int width = Mathf.Max(Screen.width / 2, 64);
            int height = Mathf.Max(Screen.height, 64);
            
            // Left eye
            leftEyeTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            leftEyeTexture.antiAliasing = QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1;
            leftEyeTexture.Create();
            leftEyeCamera.targetTexture = leftEyeTexture;
            
            // Right eye
            rightEyeTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rightEyeTexture.antiAliasing = QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1;
            rightEyeTexture.Create();
            rightEyeCamera.targetTexture = rightEyeTexture;
        }
        
        private void RecreateRenderTextures()
        {
            if (leftEyeTexture != null)
            {
                leftEyeCamera.targetTexture = null;
                leftEyeTexture.Release();
                DestroyImmediate(leftEyeTexture);
            }
            
            if (rightEyeTexture != null)
            {
                rightEyeCamera.targetTexture = null;
                rightEyeTexture.Release();
                DestroyImmediate(rightEyeTexture);
            }
            
            CreateRenderTextures();
        }
        
        private void SetupGyroscope()
        {
            gyroSupported = SystemInfo.supportsGyroscope;
            
            if (gyroSupported)
            {
                Input.gyro.enabled = true;
                Input.gyro.updateInterval = 0.0167f; // ~60Hz
                
                // Wait a frame then calibrate
                Invoke(nameof(Recenter), 0.1f);
            }
            else
            {
                Debug.LogWarning("[HUIX VR] Gyroscope not supported. Head tracking disabled.");
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleInput()
        {
            // Tap to recenter
            if (tapToRecenter)
            {
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                {
                    Recenter();
                }
                
                // Also support mouse click in editor
                #if UNITY_EDITOR
                if (Input.GetMouseButtonDown(0))
                {
                    Recenter();
                }
                #endif
            }
        }
        
        #endregion
        
        #region Head Tracking
        
        private void UpdateHeadTracking()
        {
            // Get gyro attitude and convert to Unity coordinate system
            Quaternion gyroQuat = Input.gyro.attitude;
            
            // Convert from gyro coordinate system to Unity
            // Gyro: (x, y, z, w) -> Unity: (x, y, -z, -w) then rotate
            gyroRotation = new Quaternion(gyroQuat.x, gyroQuat.y, -gyroQuat.z, -gyroQuat.w);
            
            // Apply rotation to align with Unity's coordinate system
            // Phone held in landscape: rotate 90° around X
            Quaternion rotationFix = Quaternion.Euler(90f, 0f, 0f);
            targetRotation = gyroOffset * rotationFix * gyroRotation;
            
            // Apply smoothing
            if (trackingSmoothing > 0.001f)
            {
                float t = 1f - Mathf.Pow(trackingSmoothing, Time.deltaTime * 60f);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, t);
            }
            else
            {
                transform.localRotation = targetRotation;
            }
        }
        
        /// <summary>
        /// Recenter the view to current head position
        /// </summary>
        public void Recenter()
        {
            if (!gyroSupported) return;
            
            // Store the initial rotation we want to return to
            initialRotation = Quaternion.identity;
            
            // Get current gyro reading
            Quaternion gyroQuat = Input.gyro.attitude;
            Quaternion currentGyro = new Quaternion(gyroQuat.x, gyroQuat.y, -gyroQuat.z, -gyroQuat.w);
            Quaternion rotationFix = Quaternion.Euler(90f, 0f, 0f);
            
            // Calculate offset to align current gyro reading with forward
            gyroOffset = Quaternion.Inverse(rotationFix * currentGyro);
            
            Debug.Log("[HUIX VR] View recentered");
        }
        
        #endregion
        
        #region Stereo Rendering
        
        private void UpdateEyeCameras()
        {
            if (leftEyeCamera == null || rightEyeCamera == null) return;
            
            // Update IPD
            leftEyeCamera.transform.localPosition = new Vector3(-ipd / 2f, 0, 0);
            rightEyeCamera.transform.localPosition = new Vector3(ipd / 2f, 0, 0);
            
            // Update FOV
            leftEyeCamera.fieldOfView = fieldOfView;
            rightEyeCamera.fieldOfView = fieldOfView;
        }
        
        private void RenderStereo(RenderTexture destination)
        {
            // Clear destination
            RenderTexture.active = destination;
            GL.Clear(true, true, Color.black);
            
            int halfWidth = destination.width / 2;
            int height = destination.height;
            
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, destination.width, destination.height, 0);
            
            Material renderMaterial = (enableDistortion && distortionMaterial != null) ? distortionMaterial : blitMaterial;
            
            // Update distortion parameters
            if (enableDistortion && distortionMaterial != null)
            {
                distortionMaterial.SetFloat("_K1", distortionK1);
                distortionMaterial.SetFloat("_K2", distortionK2);
                distortionMaterial.SetVector("_Center", new Vector4(0.5f, 0.5f, 0, 0));
            }
            
            // Draw left eye (bottom-left origin in GL, but we flip Y)
            Graphics.DrawTexture(new Rect(0, 0, halfWidth, height), leftEyeTexture, renderMaterial);
            
            // Draw right eye
            Graphics.DrawTexture(new Rect(halfWidth, 0, halfWidth, height), rightEyeTexture, renderMaterial);
            
            GL.PopMatrix();
            
            // Draw divider line
            if (showDivider)
            {
                DrawDivider(destination, halfWidth);
            }
            
            RenderTexture.active = null;
        }
        
        private void DrawDivider(RenderTexture destination, int centerX)
        {
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, destination.width, destination.height, 0);
            
            GL.Begin(GL.QUADS);
            GL.Color(dividerColor);
            
            int dividerHalf = 1; // 2 pixel wide divider
            GL.Vertex3(centerX - dividerHalf, 0, 0);
            GL.Vertex3(centerX + dividerHalf, 0, 0);
            GL.Vertex3(centerX + dividerHalf, destination.height, 0);
            GL.Vertex3(centerX - dividerHalf, destination.height, 0);
            
            GL.End();
            GL.PopMatrix();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Toggle VR mode on/off
        /// </summary>
        public void ToggleVR()
        {
            SetVREnabled(!vrEnabled);
        }
        
        /// <summary>
        /// Enable or disable VR mode
        /// </summary>
        public void SetVREnabled(bool enabled)
        {
            vrEnabled = enabled;
            
            if (leftEyeCamera != null) leftEyeCamera.enabled = vrEnabled;
            if (rightEyeCamera != null) rightEyeCamera.enabled = vrEnabled;
            
            if (!vrEnabled)
            {
                // Restore main camera rendering
                mainCamera.cullingMask = -1;
                mainCamera.clearFlags = CameraClearFlags.Skybox;
            }
            else
            {
                mainCamera.cullingMask = 0;
                mainCamera.clearFlags = CameraClearFlags.Nothing;
            }
        }
        
        /// <summary>
        /// Apply Google Cardboard V1 default profile
        /// </summary>
        public void ApplyCardboardV1Profile()
        {
            ipd = 0.06f;
            screenToLens = 0.042f;
            distortionK1 = 0.441f;
            distortionK2 = 0.156f;
            fieldOfView = 90f;
        }
        
        /// <summary>
        /// Apply Google Cardboard V2 default profile  
        /// </summary>
        public void ApplyCardboardV2Profile()
        {
            ipd = 0.064f;
            screenToLens = 0.039f;
            distortionK1 = 0.34f;
            distortionK2 = 0.55f;
            fieldOfView = 95f;
        }
        
        /// <summary>
        /// Get the left eye camera for advanced usage
        /// </summary>
        public Camera GetLeftEyeCamera() => leftEyeCamera;
        
        /// <summary>
        /// Get the right eye camera for advanced usage
        /// </summary>
        public Camera GetRightEyeCamera() => rightEyeCamera;
        
        /// <summary>
        /// Get the left eye transform
        /// </summary>
        public Transform GetLeftEye() => leftEyeCamera?.transform;
        
        /// <summary>
        /// Get the right eye transform
        /// </summary>
        public Transform GetRightEye() => rightEyeCamera?.transform;
        
        /// <summary>
        /// Check if head tracking is available
        /// </summary>
        public bool IsHeadTrackingAvailable() => gyroSupported;
        
        #endregion
        
        #region Cleanup
        
        private void Cleanup()
        {
            if (leftEyeTexture != null)
            {
                leftEyeTexture.Release();
                DestroyImmediate(leftEyeTexture);
            }
            
            if (rightEyeTexture != null)
            {
                rightEyeTexture.Release();
                DestroyImmediate(rightEyeTexture);
            }
            
            if (distortionMaterial != null)
            {
                DestroyImmediate(distortionMaterial);
            }
            
            if (blitMaterial != null)
            {
                DestroyImmediate(blitMaterial);
            }
            
            if (leftEyeCamera != null)
            {
                DestroyImmediate(leftEyeCamera.gameObject);
            }
            
            if (rightEyeCamera != null)
            {
                DestroyImmediate(rightEyeCamera.gameObject);
            }
        }
        
        #endregion
        
        #region Editor Helpers
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            
            if (leftEyeCamera != null)
            {
                leftEyeCamera.transform.localPosition = new Vector3(-ipd / 2f, 0, 0);
                leftEyeCamera.fieldOfView = fieldOfView;
            }
            
            if (rightEyeCamera != null)
            {
                rightEyeCamera.transform.localPosition = new Vector3(ipd / 2f, 0, 0);
                rightEyeCamera.fieldOfView = fieldOfView;
            }
        }
        
        private void Reset()
        {
            // Apply Cardboard V1 defaults when component is first added
            ApplyCardboardV1Profile();
        }
#endif
        
        #endregion
    }
}
