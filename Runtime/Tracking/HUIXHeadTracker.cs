/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Head Tracker - Device sensor-based head tracking
 */

using UnityEngine;
using System;
using HUIX.PhoneVR.Core;

namespace HUIX.PhoneVR
{
    /// <summary>
    /// Provides head tracking using device gyroscope and accelerometer.
    /// Attach to your VR camera rig to enable head rotation tracking.
    /// </summary>
    [AddComponentMenu("HUIX/Phone VR/Head Tracker")]
    public class HUIXHeadTracker : MonoBehaviour
    {
        #region Events
        public event Action OnRecenter;
        public event Action<Quaternion> OnRotationUpdated;
        #endregion

        #region Serialized Fields
        [Header("=== HUIX Head Tracker ===")]
        [Space(10)]
        
        [Header("Tracking Mode")]
        [SerializeField] private TrackingMode _trackingMode = TrackingMode.GyroscopeWithFilter;
        [SerializeField] private bool _enableTracking = true;
        
        [Header("Sensitivity")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _sensitivityX = 1f;
        [Range(0.1f, 2f)]
        [SerializeField] private float _sensitivityY = 1f;
        [Range(0.1f, 2f)]
        [SerializeField] private float _sensitivityZ = 1f;
        
        [Header("Filtering")]
        [Range(0f, 1f)]
        [SerializeField] private float _smoothing = 0.1f;
        [SerializeField] private bool _usePrediction = true;
        [Range(0f, 0.1f)]
        [SerializeField] private float _predictionTime = 0.02f;
        
        [Header("Drift Correction")]
        [SerializeField] private bool _enableDriftCorrection = true;
        [Range(0.001f, 0.1f)]
        [SerializeField] private float _driftCorrectionStrength = 0.01f;
        
        [Header("Constraints")]
        [SerializeField] private bool _limitPitch = false;
        [Range(0f, 90f)]
        [SerializeField] private float _maxPitchUp = 85f;
        [Range(0f, 90f)]
        [SerializeField] private float _maxPitchDown = 85f;
        
        [Header("Editor Simulation")]
        [SerializeField] private bool _simulateInEditor = true;
        [SerializeField] private float _mouseSimulationSpeed = 3f;
        #endregion

        #region Private Fields
        private Gyroscope _gyroscope;
        private bool _gyroSupported;
        private Quaternion _gyroRotation;
        private Quaternion _targetRotation;
        private Quaternion _smoothedRotation;
        private Quaternion _recenterOffset;
        private Quaternion _initialRotation;
        private Vector3 _angularVelocity;
        private Vector3 _lastEuler;
        private bool _isInitialized;
        
        // Editor simulation
        private float _simYaw;
        private float _simPitch;
        
        // Complementary filter
        private Vector3 _accelerometerData;
        private float _filterCoefficient = 0.98f;
        #endregion

        #region Enums
        public enum TrackingMode
        {
            GyroscopeOnly,
            GyroscopeWithFilter,
            Accelerometer,
            Combined
        }
        #endregion

        #region Properties
        public bool IsTrackingEnabled => _enableTracking;
        public bool IsGyroSupported => _gyroSupported;
        public Quaternion CurrentRotation => _smoothedRotation;
        public Vector3 AngularVelocity => _angularVelocity;
        public TrackingMode CurrentTrackingMode => _trackingMode;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!_isInitialized || !_enableTracking) return;

#if UNITY_EDITOR
            if (_simulateInEditor)
            {
                UpdateEditorSimulation();
            }
#else
            UpdateDeviceTracking();
#endif

            ApplySmoothing();
            ApplyRotation();
        }

        private void OnDisable()
        {
            if (_gyroscope != null)
            {
                _gyroscope.enabled = false;
            }
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            if (_isInitialized) return;

            _recenterOffset = Quaternion.identity;
            _initialRotation = transform.rotation;
            _smoothedRotation = Quaternion.identity;
            _targetRotation = Quaternion.identity;

#if !UNITY_EDITOR
            InitializeGyroscope();
#endif

            _isInitialized = true;
            Debug.Log($"[HUIX VR] Head Tracker initialized. Gyro supported: {_gyroSupported}");
        }

        private void InitializeGyroscope()
        {
            if (SystemInfo.supportsGyroscope)
            {
                _gyroscope = Input.gyro;
                _gyroscope.enabled = true;
                _gyroSupported = true;
                Debug.Log("[HUIX VR] Gyroscope enabled");
            }
            else
            {
                _gyroSupported = false;
                Debug.LogWarning("[HUIX VR] Gyroscope not supported on this device");
            }
        }
        #endregion

        #region Device Tracking
        private void UpdateDeviceTracking()
        {
            switch (_trackingMode)
            {
                case TrackingMode.GyroscopeOnly:
                    UpdateGyroscope();
                    break;
                    
                case TrackingMode.GyroscopeWithFilter:
                    UpdateGyroscopeWithFilter();
                    break;
                    
                case TrackingMode.Accelerometer:
                    UpdateAccelerometer();
                    break;
                    
                case TrackingMode.Combined:
                    UpdateCombined();
                    break;
            }

            // Calculate angular velocity
            Vector3 currentEuler = _targetRotation.eulerAngles;
            _angularVelocity = (currentEuler - _lastEuler) / Time.deltaTime;
            _lastEuler = currentEuler;
        }

        private void UpdateGyroscope()
        {
            if (!_gyroSupported || _gyroscope == null) return;

            // Convert gyroscope rotation to Unity coordinate system
            _gyroRotation = GyroToUnity(_gyroscope.attitude);
            _targetRotation = _recenterOffset * _gyroRotation;

            // Apply prediction
            if (_usePrediction)
            {
                Quaternion prediction = Quaternion.Euler(
                    _gyroscope.rotationRateUnbiased.x * _predictionTime * Mathf.Rad2Deg,
                    _gyroscope.rotationRateUnbiased.y * _predictionTime * Mathf.Rad2Deg,
                    _gyroscope.rotationRateUnbiased.z * _predictionTime * Mathf.Rad2Deg
                );
                _targetRotation *= prediction;
            }
        }

        private void UpdateGyroscopeWithFilter()
        {
            if (!_gyroSupported || _gyroscope == null) return;

            // Get raw gyro rotation
            Quaternion gyroRot = GyroToUnity(_gyroscope.attitude);
            
            // Get accelerometer-derived rotation for drift correction
            _accelerometerData = Input.acceleration;
            Quaternion accelRot = AccelerometerToRotation(_accelerometerData);
            
            // Complementary filter
            _gyroRotation = Quaternion.Slerp(accelRot, gyroRot, _filterCoefficient);
            _targetRotation = _recenterOffset * _gyroRotation;
        }

        private void UpdateAccelerometer()
        {
            _accelerometerData = Input.acceleration;
            Quaternion accelRot = AccelerometerToRotation(_accelerometerData);
            _targetRotation = _recenterOffset * accelRot;
        }

        private void UpdateCombined()
        {
            UpdateGyroscopeWithFilter();
            
            // Additional drift correction using gravity
            if (_enableDriftCorrection)
            {
                ApplyDriftCorrection();
            }
        }

        private Quaternion GyroToUnity(Quaternion gyro)
        {
            // Convert from right-handed to left-handed coordinate system
            return new Quaternion(gyro.x, gyro.y, -gyro.z, -gyro.w);
        }

        private Quaternion AccelerometerToRotation(Vector3 acceleration)
        {
            Vector3 gravity = acceleration.normalized;
            float pitch = Mathf.Atan2(-gravity.z, Mathf.Sqrt(gravity.x * gravity.x + gravity.y * gravity.y)) * Mathf.Rad2Deg;
            float roll = Mathf.Atan2(gravity.x, gravity.y) * Mathf.Rad2Deg;
            
            return Quaternion.Euler(pitch, 0, roll);
        }

        private void ApplyDriftCorrection()
        {
            // Use gravity vector to correct drift on pitch and roll
            Vector3 gravity = Input.acceleration.normalized;
            Vector3 up = _targetRotation * Vector3.up;
            
            float angle = Vector3.Angle(up, -gravity);
            if (angle > 1f) // Only correct if drift is noticeable
            {
                Vector3 correctionAxis = Vector3.Cross(up, -gravity).normalized;
                Quaternion correction = Quaternion.AngleAxis(angle * _driftCorrectionStrength, correctionAxis);
                _targetRotation = correction * _targetRotation;
            }
        }
        #endregion

        #region Editor Simulation
        private void UpdateEditorSimulation()
        {
            // Mouse look simulation when right mouse button is held
            if (Input.GetMouseButton(1))
            {
                _simYaw += Input.GetAxis("Mouse X") * _mouseSimulationSpeed;
                _simPitch -= Input.GetAxis("Mouse Y") * _mouseSimulationSpeed;
                _simPitch = Mathf.Clamp(_simPitch, -89f, 89f);
            }

            // Keyboard simulation
            if (Input.GetKey(KeyCode.LeftArrow)) _simYaw -= _mouseSimulationSpeed * 20f * Time.deltaTime;
            if (Input.GetKey(KeyCode.RightArrow)) _simYaw += _mouseSimulationSpeed * 20f * Time.deltaTime;
            if (Input.GetKey(KeyCode.UpArrow)) _simPitch -= _mouseSimulationSpeed * 20f * Time.deltaTime;
            if (Input.GetKey(KeyCode.DownArrow)) _simPitch += _mouseSimulationSpeed * 20f * Time.deltaTime;

            _targetRotation = Quaternion.Euler(_simPitch, _simYaw, 0) * _recenterOffset;
        }
        #endregion

        #region Smoothing & Application
        private void ApplySmoothing()
        {
            if (_smoothing > 0)
            {
                _smoothedRotation = Quaternion.Slerp(_smoothedRotation, _targetRotation, Time.deltaTime / _smoothing);
            }
            else
            {
                _smoothedRotation = _targetRotation;
            }
        }

        private void ApplyRotation()
        {
            // Apply sensitivity
            Vector3 euler = _smoothedRotation.eulerAngles;
            euler.x *= _sensitivityX;
            euler.y *= _sensitivityY;
            euler.z *= _sensitivityZ;

            // Apply pitch constraints
            if (_limitPitch)
            {
                euler.x = ClampAngle(euler.x, -_maxPitchDown, _maxPitchUp);
            }

            // Apply final rotation
            transform.rotation = _initialRotation * Quaternion.Euler(euler);
            
            OnRotationUpdated?.Invoke(transform.rotation);
        }

        private float ClampAngle(float angle, float min, float max)
        {
            if (angle > 180f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Enable or disable head tracking
        /// </summary>
        public void EnableTracking(bool enable)
        {
            _enableTracking = enable;

            if (_gyroscope != null)
            {
                _gyroscope.enabled = enable;
            }

            Debug.Log($"[HUIX VR] Head tracking {(enable ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Recenter the view to current head position
        /// </summary>
        public void Recenter()
        {
            if (_gyroSupported && _gyroscope != null)
            {
                _recenterOffset = Quaternion.Inverse(GyroToUnity(_gyroscope.attitude));
            }
            
            _simYaw = 0;
            _simPitch = 0;
            
            OnRecenter?.Invoke();
            Debug.Log("[HUIX VR] View recentered");
        }

        /// <summary>
        /// Set the tracking mode
        /// </summary>
        public void SetTrackingMode(TrackingMode mode)
        {
            _trackingMode = mode;
            Debug.Log($"[HUIX VR] Tracking mode set to: {mode}");
        }

        /// <summary>
        /// Calibrate the sensors (should be done when device is stationary)
        /// </summary>
        public void Calibrate()
        {
            Debug.Log("[HUIX VR] Calibrating sensors...");
            Recenter();
            // Additional calibration could be added here
        }

        /// <summary>
        /// Get Euler angles of current rotation
        /// </summary>
        public Vector3 GetEulerAngles()
        {
            return _smoothedRotation.eulerAngles;
        }

        /// <summary>
        /// Get the forward direction of the head
        /// </summary>
        public Vector3 GetForward()
        {
            return transform.forward;
        }
        #endregion
    }
}

