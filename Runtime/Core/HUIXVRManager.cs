/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Core VR Manager - The heart of the HUIX Phone VR system
 */

using UnityEngine;
using System;

namespace HUIX.PhoneVR.Core
{
    /// <summary>
    /// Main manager for the HUIX Phone VR SDK.
    /// Handles initialization, configuration, and coordination of all VR subsystems.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class HUIXVRManager : MonoBehaviour
    {
        #region Singleton
        private static HUIXVRManager _instance;
        public static HUIXVRManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<HUIXVRManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("HUIX VR Manager");
                        _instance = go.AddComponent<HUIXVRManager>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Events
        public static event Action OnVRInitialized;
        public static event Action OnVRModeEnabled;
        public static event Action OnVRModeDisabled;
        public static event Action<HeadsetProfile> OnHeadsetChanged;
        #endregion

        #region Serialized Fields
        [Header("=== HUIX PHONE VR SDK ===")]
        [Space(10)]
        
        [Header("General Settings")]
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private bool _vrModeOnStart = true;
        [SerializeField] private bool _persistAcrossScenes = true;
        
        [Header("Headset Configuration")]
        [SerializeField] private HeadsetProfile _headsetProfile;
        
        [Header("Performance")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private bool _adaptiveQuality = true;
        [SerializeField] private float _renderScale = 1.0f;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;
        [SerializeField] private bool _simulateInEditor = true;
        #endregion

        #region Properties
        public bool IsInitialized { get; private set; }
        public bool IsVRModeActive { get; private set; }
        public HeadsetProfile CurrentHeadset => _headsetProfile;
        public float RenderScale => _renderScale;
        public bool AdaptiveQuality => _adaptiveQuality;
        public bool ShowDebugInfo => _showDebugInfo;
        public bool SimulateInEditor => _simulateInEditor;
        #endregion

        #region Private Fields
        private HUIXVRCamera _vrCamera;
        private HUIXHeadTracker _headTracker;
        private HUIXInputManager _inputManager;
        private float _lastFrameTime;
        private int _frameCount;
        private float _currentFPS;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (_persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (_autoInitialize)
            {
                Initialize();
            }
        }

        private void Start()
        {
            if (_vrModeOnStart && IsInitialized)
            {
                EnableVRMode();
            }
        }

        private void Update()
        {
            if (!IsInitialized || !IsVRModeActive) return;

            UpdateFPSCounter();
            
            if (_adaptiveQuality)
            {
                UpdateAdaptiveQuality();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the HUIX VR system
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[HUIX VR] Already initialized!");
                return;
            }

            Debug.Log("[HUIX VR] Initializing HUIX Phone VR SDK v1.0.0...");

            // Set target frame rate
            Application.targetFrameRate = _targetFrameRate;
            
            // Prevent screen dimming
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Load default headset profile if none assigned
            if (_headsetProfile == null)
            {
                _headsetProfile = HeadsetProfile.CreateDefault();
            }

            // Initialize subsystems
            InitializeSubsystems();

            IsInitialized = true;
            Debug.Log("[HUIX VR] Initialization complete!");
            
            OnVRInitialized?.Invoke();
        }

        private void InitializeSubsystems()
        {
            // Find or create VR Camera
            _vrCamera = FindObjectOfType<HUIXVRCamera>();
            if (_vrCamera == null)
            {
                Debug.Log("[HUIX VR] Creating VR Camera...");
            }

            // Find or create Head Tracker
            _headTracker = FindObjectOfType<HUIXHeadTracker>();
            if (_headTracker == null)
            {
                Debug.Log("[HUIX VR] Creating Head Tracker...");
            }

            // Find or create Input Manager
            _inputManager = FindObjectOfType<HUIXInputManager>();
            if (_inputManager == null)
            {
                Debug.Log("[HUIX VR] Creating Input Manager...");
            }
        }
        #endregion

        #region VR Mode Control
        /// <summary>
        /// Enable VR stereoscopic mode
        /// </summary>
        public void EnableVRMode()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[HUIX VR] Cannot enable VR mode - not initialized!");
                return;
            }

            if (IsVRModeActive) return;

            Debug.Log("[HUIX VR] Enabling VR Mode...");
            
            // Set landscape orientation
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            // Enable VR camera
            if (_vrCamera != null)
            {
                _vrCamera.EnableStereoRendering(true);
            }

            // Enable head tracking
            if (_headTracker != null)
            {
                _headTracker.EnableTracking(true);
            }

            IsVRModeActive = true;
            OnVRModeEnabled?.Invoke();
        }

        /// <summary>
        /// Disable VR mode and return to normal view
        /// </summary>
        public void DisableVRMode()
        {
            if (!IsVRModeActive) return;

            Debug.Log("[HUIX VR] Disabling VR Mode...");

            // Disable VR camera
            if (_vrCamera != null)
            {
                _vrCamera.EnableStereoRendering(false);
            }

            // Disable head tracking
            if (_headTracker != null)
            {
                _headTracker.EnableTracking(false);
            }

            Screen.orientation = ScreenOrientation.AutoRotation;

            IsVRModeActive = false;
            OnVRModeDisabled?.Invoke();
        }

        /// <summary>
        /// Toggle VR mode on/off
        /// </summary>
        public void ToggleVRMode()
        {
            if (IsVRModeActive)
                DisableVRMode();
            else
                EnableVRMode();
        }
        #endregion

        #region Headset Configuration
        /// <summary>
        /// Set the current headset profile
        /// </summary>
        public void SetHeadsetProfile(HeadsetProfile profile)
        {
            if (profile == null)
            {
                Debug.LogError("[HUIX VR] Cannot set null headset profile!");
                return;
            }

            _headsetProfile = profile;
            
            // Update VR camera with new profile
            if (_vrCamera != null)
            {
                _vrCamera.ApplyHeadsetProfile(profile);
            }

            OnHeadsetChanged?.Invoke(profile);
            Debug.Log($"[HUIX VR] Headset profile changed to: {profile.ProfileName}");
        }

        /// <summary>
        /// Adjust IPD (Inter-Pupillary Distance)
        /// </summary>
        public void SetIPD(float ipd)
        {
            if (_headsetProfile != null)
            {
                _headsetProfile.IPD = Mathf.Clamp(ipd, 50f, 80f);
                SetHeadsetProfile(_headsetProfile);
            }
        }
        #endregion

        #region Performance
        private void UpdateFPSCounter()
        {
            _frameCount++;
            float elapsed = Time.realtimeSinceStartup - _lastFrameTime;
            
            if (elapsed >= 1f)
            {
                _currentFPS = _frameCount / elapsed;
                _frameCount = 0;
                _lastFrameTime = Time.realtimeSinceStartup;
            }
        }

        private void UpdateAdaptiveQuality()
        {
            // Dynamically adjust render scale based on performance
            if (_currentFPS < _targetFrameRate * 0.8f && _renderScale > 0.5f)
            {
                _renderScale = Mathf.Max(0.5f, _renderScale - 0.05f * Time.deltaTime);
            }
            else if (_currentFPS >= _targetFrameRate * 0.95f && _renderScale < 1f)
            {
                _renderScale = Mathf.Min(1f, _renderScale + 0.02f * Time.deltaTime);
            }
        }

        public float GetCurrentFPS() => _currentFPS;
        #endregion

        #region Recenter
        /// <summary>
        /// Recenter the VR view to current head position
        /// </summary>
        public void Recenter()
        {
            if (_headTracker != null)
            {
                _headTracker.Recenter();
            }
            Debug.Log("[HUIX VR] View recentered");
        }
        #endregion

        #region Debug GUI
        private void OnGUI()
        {
            if (!_showDebugInfo || !IsVRModeActive) return;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                normal = { textColor = Color.green }
            };

            float y = 10;
            float lineHeight = 30;

            GUI.Label(new Rect(10, y, 500, lineHeight), $"HUIX Phone VR SDK v1.0.0", style);
            y += lineHeight;
            GUI.Label(new Rect(10, y, 500, lineHeight), $"FPS: {_currentFPS:F1}", style);
            y += lineHeight;
            GUI.Label(new Rect(10, y, 500, lineHeight), $"Render Scale: {_renderScale:P0}", style);
            y += lineHeight;
            
            if (_headsetProfile != null)
            {
                GUI.Label(new Rect(10, y, 500, lineHeight), $"Headset: {_headsetProfile.ProfileName}", style);
                y += lineHeight;
                GUI.Label(new Rect(10, y, 500, lineHeight), $"IPD: {_headsetProfile.IPD:F1}mm", style);
            }
        }
        #endregion
    }
}

