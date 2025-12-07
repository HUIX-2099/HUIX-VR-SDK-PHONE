/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * VR Button - Interactive button for VR UI
 */

using UnityEngine;
using UnityEngine.Events;

namespace HUIX.PhoneVR.UI
{
    /// <summary>
    /// VR-optimized button that responds to gaze and trigger input.
    /// </summary>
    [AddComponentMenu("HUIX/Phone VR/UI/VR Button")]
    public class HUIXVRButton : MonoBehaviour, IHUIXInteractable
    {
        #region Serialized Fields
        [Header("=== HUIX VR Button ===")]
        [Space(10)]
        
        [Header("Visual Settings")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _hoverColor = new Color(0.8f, 0.9f, 1f, 1f);
        [SerializeField] private Color _pressedColor = new Color(0.5f, 0.7f, 1f, 1f);
        [SerializeField] private Color _disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
        [Header("Animation")]
        [SerializeField] private float _hoverScale = 1.1f;
        [SerializeField] private float _pressedScale = 0.95f;
        [SerializeField] private float _animationSpeed = 10f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _hoverSound;
        [SerializeField] private AudioClip _clickSound;
        [SerializeField] private float _volume = 0.5f;
        
        [Header("State")]
        [SerializeField] private bool _interactable = true;
        
        [Header("Events")]
        [SerializeField] private UnityEvent _onClick;
        [SerializeField] private UnityEvent _onGazeEnter;
        [SerializeField] private UnityEvent _onGazeExit;
        #endregion

        #region Private Fields
        private Renderer _renderer;
        private Material _material;
        private AudioSource _audioSource;
        private Vector3 _originalScale;
        private float _targetScale = 1f;
        private Color _targetColor;
        private bool _isGazing;
        private bool _isPressed;
        #endregion

        #region Properties
        public bool Interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                UpdateVisual();
            }
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer != null)
            {
                _material = _renderer.material;
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f; // 3D sound
            }

            _originalScale = transform.localScale;
            _targetColor = _normalColor;
        }

        private void Update()
        {
            // Animate scale
            Vector3 targetScaleVec = _originalScale * _targetScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScaleVec, Time.deltaTime * _animationSpeed);

            // Animate color
            if (_material != null)
            {
                _material.color = Color.Lerp(_material.color, _targetColor, Time.deltaTime * _animationSpeed);
            }
        }

        private void OnEnable()
        {
            // Subscribe to input events
            HUIXInputManager.OnTriggerDown += HandleTriggerDown;
            HUIXInputManager.OnTriggerUp += HandleTriggerUp;
        }

        private void OnDisable()
        {
            // Unsubscribe from input events
            HUIXInputManager.OnTriggerDown -= HandleTriggerDown;
            HUIXInputManager.OnTriggerUp -= HandleTriggerUp;
        }
        #endregion

        #region IHUIXInteractable Implementation
        public void OnGazeEnter()
        {
            if (!_interactable) return;

            _isGazing = true;
            _targetScale = _hoverScale;
            _targetColor = _hoverColor;

            PlaySound(_hoverSound);
            _onGazeEnter?.Invoke();
        }

        public void OnGazeExit()
        {
            _isGazing = false;
            _isPressed = false;
            _targetScale = 1f;
            _targetColor = _interactable ? _normalColor : _disabledColor;

            _onGazeExit?.Invoke();
        }

        public void OnSelect()
        {
            if (!_interactable) return;

            PlaySound(_clickSound);
            _onClick?.Invoke();

            // Visual feedback
            _targetColor = _pressedColor;
            _targetScale = _pressedScale;

            // Haptic feedback
            HUIXInputManager inputManager = FindObjectOfType<HUIXInputManager>();
            inputManager?.TriggerHaptics();
        }
        #endregion

        #region Input Handlers
        private void HandleTriggerDown()
        {
            if (!_isGazing || !_interactable) return;

            _isPressed = true;
            _targetScale = _pressedScale;
            _targetColor = _pressedColor;
        }

        private void HandleTriggerUp()
        {
            if (!_isPressed) return;

            _isPressed = false;

            if (_isGazing && _interactable)
            {
                _targetScale = _hoverScale;
                _targetColor = _hoverColor;
            }
            else
            {
                _targetScale = 1f;
                _targetColor = _interactable ? _normalColor : _disabledColor;
            }
        }
        #endregion

        #region Helper Methods
        private void UpdateVisual()
        {
            if (!_interactable)
            {
                _targetColor = _disabledColor;
                _targetScale = 1f;
            }
            else if (_isGazing)
            {
                _targetColor = _hoverColor;
                _targetScale = _hoverScale;
            }
            else
            {
                _targetColor = _normalColor;
                _targetScale = 1f;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip, _volume);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Programmatically click the button
        /// </summary>
        public void Click()
        {
            if (_interactable)
            {
                OnSelect();
            }
        }

        /// <summary>
        /// Add click listener
        /// </summary>
        public void AddClickListener(UnityAction action)
        {
            _onClick.AddListener(action);
        }

        /// <summary>
        /// Remove click listener
        /// </summary>
        public void RemoveClickListener(UnityAction action)
        {
            _onClick.RemoveListener(action);
        }
        #endregion
    }
}

