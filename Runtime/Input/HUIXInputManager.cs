/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Input Manager - Handle VR input methods
 */

using UnityEngine;
using UnityEngine.Events;
using System;
using HUIX.PhoneVR.Core;

namespace HUIX.PhoneVR
{
    /// <summary>
    /// Manages VR input including gaze, screen tap, and controller support.
    /// </summary>
    [AddComponentMenu("HUIX/Phone VR/Input Manager")]
    public class HUIXInputManager : MonoBehaviour
    {
        #region Events
        public static event Action OnTriggerDown;
        public static event Action OnTriggerUp;
        public static event Action OnBackButton;
        public static event Action OnRecenterButton;
        public static event Action<GameObject> OnGazeEnter;
        public static event Action<GameObject> OnGazeExit;
        public static event Action<GameObject> OnGazeSelect;
        #endregion

        #region Serialized Fields
        [Header("=== HUIX Input Manager ===")]
        [Space(10)]
        
        [Header("Input Sources")]
        [SerializeField] private bool _enableScreenTap = true;
        [SerializeField] private bool _enableMagnetTrigger = true;
        [SerializeField] private bool _enableVolumeButtons = false;
        [SerializeField] private bool _enableBluetoothController = true;
        
        [Header("Gaze Input")]
        [SerializeField] private bool _enableGazeInput = true;
        [SerializeField] private float _gazeDistance = 100f;
        [SerializeField] private LayerMask _gazeLayerMask = -1;
        [SerializeField] private float _gazeDwellTime = 2f;
        [SerializeField] private bool _autoSelectOnDwell = true;
        
        [Header("Reticle")]
        [SerializeField] private bool _showReticle = true;
        [SerializeField] private HUIXReticle _reticle;
        
        [Header("Trigger Settings")]
        [SerializeField] private float _triggerHoldTime = 0f;
        [SerializeField] private float _doubleTapWindow = 0.3f;
        
        [Header("Unity Events")]
        [SerializeField] private UnityEvent _onTriggerPressed;
        [SerializeField] private UnityEvent _onTriggerReleased;
        #endregion

        #region Private Fields
        private Camera _vrCamera;
        private GameObject _currentGazedObject;
        private GameObject _previousGazedObject;
        private float _gazeStartTime;
        private float _lastTapTime;
        private bool _isTriggerDown;
        private bool _isDwelling;
        
        // Magnetic trigger detection
        private Vector3 _lastMagneticField;
        private float _magneticThreshold = 50f;
        #endregion

        #region Properties
        public bool IsTriggerDown => _isTriggerDown;
        public GameObject CurrentGazedObject => _currentGazedObject;
        public bool IsGazing => _currentGazedObject != null;
        public float GazeDwellProgress => _isDwelling ? (Time.time - _gazeStartTime) / _gazeDwellTime : 0f;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (_enableGazeInput)
            {
                UpdateGaze();
            }

            UpdateInputSources();
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            // Find VR camera
            HUIXVRCamera vrCam = FindObjectOfType<HUIXVRCamera>();
            if (vrCam != null)
            {
                _vrCamera = vrCam.GetComponent<Camera>();
            }
            else
            {
                _vrCamera = Camera.main;
            }

            // Create reticle if needed
            if (_showReticle && _reticle == null)
            {
                CreateDefaultReticle();
            }

            // Initialize magnetic field reading
            Input.compass.enabled = _enableMagnetTrigger;

            Debug.Log("[HUIX VR] Input Manager initialized");
        }

        private void CreateDefaultReticle()
        {
            GameObject reticleObj = new GameObject("HUIX Reticle");
            _reticle = reticleObj.AddComponent<HUIXReticle>();
            
            if (_vrCamera != null)
            {
                reticleObj.transform.SetParent(_vrCamera.transform);
            }
        }
        #endregion

        #region Gaze System
        private void UpdateGaze()
        {
            if (_vrCamera == null) return;

            Ray gazeRay = new Ray(_vrCamera.transform.position, _vrCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(gazeRay, out hit, _gazeDistance, _gazeLayerMask))
            {
                ProcessGazeHit(hit);
            }
            else
            {
                ProcessGazeNoHit();
            }

            // Update reticle
            if (_reticle != null)
            {
                _reticle.UpdateReticle(gazeRay, hit, _currentGazedObject != null);
            }
        }

        private void ProcessGazeHit(RaycastHit hit)
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject != _currentGazedObject)
            {
                // Gaze changed to new object
                if (_currentGazedObject != null)
                {
                    OnGazeExit?.Invoke(_currentGazedObject);
                    
                    // Notify interactable
                    IHUIXInteractable interactable = _currentGazedObject.GetComponent<IHUIXInteractable>();
                    interactable?.OnGazeExit();
                }

                _previousGazedObject = _currentGazedObject;
                _currentGazedObject = hitObject;
                _gazeStartTime = Time.time;
                _isDwelling = true;

                OnGazeEnter?.Invoke(_currentGazedObject);
                
                // Notify new interactable
                IHUIXInteractable newInteractable = _currentGazedObject.GetComponent<IHUIXInteractable>();
                newInteractable?.OnGazeEnter();
            }
            else if (_isDwelling && _autoSelectOnDwell)
            {
                // Check for dwell select
                if (Time.time - _gazeStartTime >= _gazeDwellTime)
                {
                    PerformGazeSelect();
                    _isDwelling = false;
                }
            }

            // Update reticle position
            if (_reticle != null)
            {
                _reticle.SetPosition(hit.point, hit.normal);
            }
        }

        private void ProcessGazeNoHit()
        {
            if (_currentGazedObject != null)
            {
                OnGazeExit?.Invoke(_currentGazedObject);
                
                IHUIXInteractable interactable = _currentGazedObject.GetComponent<IHUIXInteractable>();
                interactable?.OnGazeExit();
                
                _previousGazedObject = _currentGazedObject;
                _currentGazedObject = null;
                _isDwelling = false;
            }

            // Reset reticle to default position
            if (_reticle != null)
            {
                _reticle.SetDefaultPosition(_gazeDistance);
            }
        }

        private void PerformGazeSelect()
        {
            if (_currentGazedObject == null) return;

            OnGazeSelect?.Invoke(_currentGazedObject);

            IHUIXInteractable interactable = _currentGazedObject.GetComponent<IHUIXInteractable>();
            interactable?.OnSelect();

            // Reset dwell
            _gazeStartTime = Time.time;
        }
        #endregion

        #region Input Sources
        private void UpdateInputSources()
        {
            // Screen tap (touch input)
            if (_enableScreenTap)
            {
                UpdateScreenTap();
            }

            // Magnetic trigger (Cardboard style)
            if (_enableMagnetTrigger)
            {
                UpdateMagneticTrigger();
            }

            // Bluetooth controller / keyboard
            if (_enableBluetoothController)
            {
                UpdateControllerInput();
            }

            // Back button
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnBackButton?.Invoke();
            }
        }

        private void UpdateScreenTap()
        {
            // Touch input
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                if (touch.phase == TouchPhase.Began)
                {
                    TriggerDown();
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    TriggerUp();
                }
            }

            // Mouse input (for editor testing)
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                TriggerDown();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                TriggerUp();
            }
#endif
        }

        private void UpdateMagneticTrigger()
        {
            Vector3 currentMagnetic = Input.compass.rawVector;
            float magneticDelta = (currentMagnetic - _lastMagneticField).magnitude;

            if (magneticDelta > _magneticThreshold)
            {
                TriggerDown();
                // Auto release after short delay
                Invoke(nameof(TriggerUp), 0.1f);
            }

            _lastMagneticField = currentMagnetic;
        }

        private void UpdateControllerInput()
        {
            // Gamepad / Bluetooth controller
            if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                TriggerDown();
            }

            if (Input.GetButtonUp("Fire1") || Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.Return))
            {
                TriggerUp();
            }

            // Recenter
            if (Input.GetKeyDown(KeyCode.R) || Input.GetButtonDown("Fire2"))
            {
                OnRecenterButton?.Invoke();
                if (HUIXVRManager.Instance != null)
                {
                    HUIXVRManager.Instance.Recenter();
                }
            }
        }
        #endregion

        #region Trigger Methods
        private void TriggerDown()
        {
            if (_isTriggerDown) return;
            
            _isTriggerDown = true;
            
            // Check for double tap
            float timeSinceLastTap = Time.time - _lastTapTime;
            bool isDoubleTap = timeSinceLastTap <= _doubleTapWindow;
            _lastTapTime = Time.time;

            OnTriggerDown?.Invoke();
            _onTriggerPressed?.Invoke();

            // Perform gaze select if gazing at an object
            if (_currentGazedObject != null)
            {
                PerformGazeSelect();
            }

            // Double tap to recenter
            if (isDoubleTap)
            {
                OnRecenterButton?.Invoke();
            }
        }

        private void TriggerUp()
        {
            if (!_isTriggerDown) return;
            
            _isTriggerDown = false;
            
            OnTriggerUp?.Invoke();
            _onTriggerReleased?.Invoke();
        }

        /// <summary>
        /// Simulate a trigger press (for external input)
        /// </summary>
        public void SimulateTrigger()
        {
            TriggerDown();
            Invoke(nameof(TriggerUp), 0.1f);
        }
        #endregion

        #region Haptic Feedback
        /// <summary>
        /// Trigger haptic feedback (vibration)
        /// </summary>
        public void TriggerHaptics(float duration = 0.05f)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
        #endregion
    }

    /// <summary>
    /// Interface for objects that can be interacted with using gaze/trigger
    /// </summary>
    public interface IHUIXInteractable
    {
        void OnGazeEnter();
        void OnGazeExit();
        void OnSelect();
    }
}

