/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * VR Canvas - World-space canvas optimized for VR
 */

using UnityEngine;
using HUIX.PhoneVR.Core;

namespace HUIX.PhoneVR.UI
{
    /// <summary>
    /// Creates and manages a world-space UI canvas optimized for VR viewing.
    /// </summary>
    [AddComponentMenu("HUIX/Phone VR/UI/VR Canvas")]
    [RequireComponent(typeof(Canvas))]
    public class HUIXVRCanvas : MonoBehaviour
    {
        #region Serialized Fields
        [Header("=== HUIX VR Canvas ===")]
        [Space(10)]
        
        [Header("Placement")]
        [SerializeField] private float _distance = 3f;
        [SerializeField] private float _width = 2f;
        [SerializeField] private float _height = 1.5f;
        [SerializeField] private bool _followCamera = false;
        [SerializeField] private float _followSpeed = 5f;
        
        [Header("Behavior")]
        [SerializeField] private bool _faceCamera = true;
        [SerializeField] private bool _autoPlace = true;
        [SerializeField] private CanvasPlacement _placement = CanvasPlacement.InFrontOfCamera;
        
        [Header("Rendering")]
        [SerializeField] private int _sortOrder = 0;
        [SerializeField] private float _pixelsPerUnit = 100f;
        
        [Header("Curved Canvas")]
        [SerializeField] private bool _curved = false;
        [SerializeField] private float _curveAngle = 30f;
        #endregion

        #region Private Fields
        private Canvas _canvas;
        private RectTransform _rectTransform;
        private Camera _vrCamera;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        #endregion

        #region Enums
        public enum CanvasPlacement
        {
            InFrontOfCamera,
            FixedPosition,
            AttachedToCamera
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _rectTransform = GetComponent<RectTransform>();

            SetupCanvas();
        }

        private void Start()
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

            if (_autoPlace)
            {
                PlaceCanvas();
            }
        }

        private void LateUpdate()
        {
            if (_vrCamera == null) return;

            if (_followCamera)
            {
                UpdateFollowCamera();
            }
            else if (_faceCamera && _placement != CanvasPlacement.AttachedToCamera)
            {
                FaceCamera();
            }
        }
        #endregion

        #region Setup
        private void SetupCanvas()
        {
            // Configure canvas for world space
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.worldCamera = _vrCamera;
            _canvas.sortingOrder = _sortOrder;

            // Set size
            _rectTransform.sizeDelta = new Vector2(_width * _pixelsPerUnit, _height * _pixelsPerUnit);
            
            // Scale to world units
            float scale = 1f / _pixelsPerUnit;
            transform.localScale = new Vector3(scale, scale, scale);
        }

        private void PlaceCanvas()
        {
            if (_vrCamera == null) return;

            switch (_placement)
            {
                case CanvasPlacement.InFrontOfCamera:
                    transform.position = _vrCamera.transform.position + _vrCamera.transform.forward * _distance;
                    transform.rotation = Quaternion.LookRotation(transform.position - _vrCamera.transform.position);
                    break;

                case CanvasPlacement.AttachedToCamera:
                    transform.SetParent(_vrCamera.transform);
                    transform.localPosition = new Vector3(0, 0, _distance);
                    transform.localRotation = Quaternion.identity;
                    break;

                case CanvasPlacement.FixedPosition:
                    // Keep current position
                    break;
            }

            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
        }
        #endregion

        #region Update Methods
        private void UpdateFollowCamera()
        {
            // Calculate target position in front of camera
            _targetPosition = _vrCamera.transform.position + _vrCamera.transform.forward * _distance;
            
            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _followSpeed);
            
            // Face camera
            FaceCamera();
        }

        private void FaceCamera()
        {
            if (_vrCamera == null) return;

            Vector3 lookDirection = transform.position - _vrCamera.transform.position;
            if (lookDirection != Vector3.zero)
            {
                _targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _followSpeed);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Reposition canvas in front of current view
        /// </summary>
        public void Reposition()
        {
            PlaceCanvas();
        }

        /// <summary>
        /// Set canvas distance from camera
        /// </summary>
        public void SetDistance(float distance)
        {
            _distance = Mathf.Max(0.5f, distance);
            if (_autoPlace)
            {
                PlaceCanvas();
            }
        }

        /// <summary>
        /// Set canvas size
        /// </summary>
        public void SetSize(float width, float height)
        {
            _width = width;
            _height = height;
            _rectTransform.sizeDelta = new Vector2(_width * _pixelsPerUnit, _height * _pixelsPerUnit);
        }

        /// <summary>
        /// Enable/disable camera following
        /// </summary>
        public void SetFollowCamera(bool follow)
        {
            _followCamera = follow;
        }
        #endregion

        #region Editor
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(_width, _height, 0.01f));
        }
        #endregion
    }
}

