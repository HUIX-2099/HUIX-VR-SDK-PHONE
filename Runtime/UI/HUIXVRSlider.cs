/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * VR Slider - Interactive slider for VR UI
 */

using UnityEngine;
using UnityEngine.Events;

namespace HUIX.PhoneVR.UI
{
    /// <summary>
    /// VR-optimized slider that responds to gaze-based interaction.
    /// </summary>
    [AddComponentMenu("HUIX/Phone VR/UI/VR Slider")]
    public class HUIXVRSlider : MonoBehaviour, IHUIXInteractable
    {
        #region Serialized Fields
        [Header("=== HUIX VR Slider ===")]
        [Space(10)]
        
        [Header("Value")]
        [SerializeField] private float _value = 0.5f;
        [SerializeField] private float _minValue = 0f;
        [SerializeField] private float _maxValue = 1f;
        [SerializeField] private bool _wholeNumbers = false;
        
        [Header("Visual Components")]
        [SerializeField] private Transform _handle;
        [SerializeField] private Transform _fillArea;
        [SerializeField] private Transform _background;
        
        [Header("Colors")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _hoverColor = new Color(0.8f, 0.9f, 1f, 1f);
        [SerializeField] private Color _fillColor = new Color(0.2f, 0.6f, 1f, 1f);
        
        [Header("Interaction")]
        [SerializeField] private float _sensitivity = 0.01f;
        [SerializeField] private bool _interactable = true;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<float> _onValueChanged;
        #endregion

        #region Private Fields
        private Renderer _handleRenderer;
        private Material _handleMaterial;
        private bool _isGazing;
        private bool _isDragging;
        private Vector3 _startGazePosition;
        private float _startValue;
        private float _sliderLength = 1f;
        #endregion

        #region Properties
        public float Value
        {
            get => _value;
            set => SetValue(value);
        }

        public float NormalizedValue
        {
            get => (_value - _minValue) / (_maxValue - _minValue);
            set => SetValue(Mathf.Lerp(_minValue, _maxValue, value));
        }

        public bool Interactable
        {
            get => _interactable;
            set => _interactable = value;
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_handle != null)
            {
                _handleRenderer = _handle.GetComponent<Renderer>();
                if (_handleRenderer != null)
                {
                    _handleMaterial = _handleRenderer.material;
                }
            }

            if (_background != null)
            {
                _sliderLength = _background.localScale.x;
            }
        }

        private void Start()
        {
            UpdateVisuals();
        }

        private void OnEnable()
        {
            HUIXInputManager.OnTriggerDown += HandleTriggerDown;
            HUIXInputManager.OnTriggerUp += HandleTriggerUp;
        }

        private void OnDisable()
        {
            HUIXInputManager.OnTriggerDown -= HandleTriggerDown;
            HUIXInputManager.OnTriggerUp -= HandleTriggerUp;
        }

        private void Update()
        {
            if (_isDragging)
            {
                UpdateDrag();
            }
        }
        #endregion

        #region IHUIXInteractable Implementation
        public void OnGazeEnter()
        {
            if (!_interactable) return;

            _isGazing = true;
            
            if (_handleMaterial != null)
            {
                _handleMaterial.color = _hoverColor;
            }
        }

        public void OnGazeExit()
        {
            _isGazing = false;
            _isDragging = false;

            if (_handleMaterial != null)
            {
                _handleMaterial.color = _normalColor;
            }
        }

        public void OnSelect()
        {
            // Slider doesn't use direct select - uses drag instead
        }
        #endregion

        #region Input Handlers
        private void HandleTriggerDown()
        {
            if (!_isGazing || !_interactable) return;

            _isDragging = true;
            _startValue = _value;
            
            // Get initial gaze position
            HUIXInputManager inputManager = FindObjectOfType<HUIXInputManager>();
            if (inputManager != null)
            {
                Camera vrCamera = Camera.main;
                if (vrCamera != null)
                {
                    _startGazePosition = vrCamera.transform.forward;
                }
            }
        }

        private void HandleTriggerUp()
        {
            _isDragging = false;
        }
        #endregion

        #region Drag Logic
        private void UpdateDrag()
        {
            Camera vrCamera = Camera.main;
            if (vrCamera == null) return;

            // Calculate horizontal movement based on head rotation
            Vector3 currentGazeDirection = vrCamera.transform.forward;
            Vector3 localForward = transform.InverseTransformDirection(currentGazeDirection);
            Vector3 localStart = transform.InverseTransformDirection(_startGazePosition);
            
            float delta = (localForward.x - localStart.x) * _sensitivity * 100f;
            
            SetValue(_startValue + delta * (_maxValue - _minValue));
        }
        #endregion

        #region Value Methods
        private void SetValue(float newValue)
        {
            // Clamp to range
            newValue = Mathf.Clamp(newValue, _minValue, _maxValue);
            
            // Round to whole numbers if needed
            if (_wholeNumbers)
            {
                newValue = Mathf.Round(newValue);
            }

            if (Mathf.Approximately(_value, newValue)) return;

            _value = newValue;
            UpdateVisuals();
            _onValueChanged?.Invoke(_value);
        }

        private void UpdateVisuals()
        {
            float normalized = NormalizedValue;

            // Update handle position
            if (_handle != null)
            {
                float halfLength = _sliderLength / 2f;
                float handleX = Mathf.Lerp(-halfLength, halfLength, normalized);
                _handle.localPosition = new Vector3(handleX, _handle.localPosition.y, _handle.localPosition.z);
            }

            // Update fill area
            if (_fillArea != null)
            {
                Vector3 scale = _fillArea.localScale;
                scale.x = normalized * _sliderLength;
                _fillArea.localScale = scale;
                
                // Position fill to start from left
                float halfFill = scale.x / 2f;
                float halfLength = _sliderLength / 2f;
                _fillArea.localPosition = new Vector3(-halfLength + halfFill, _fillArea.localPosition.y, _fillArea.localPosition.z);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add value changed listener
        /// </summary>
        public void AddValueChangedListener(UnityAction<float> action)
        {
            _onValueChanged.AddListener(action);
        }

        /// <summary>
        /// Remove value changed listener
        /// </summary>
        public void RemoveValueChangedListener(UnityAction<float> action)
        {
            _onValueChanged.RemoveListener(action);
        }
        #endregion
    }
}

