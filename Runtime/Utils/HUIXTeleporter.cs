/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Teleporter - Gaze-based teleportation locomotion
 */

using UnityEngine;
using System.Collections;

namespace HUIX.PhoneVR
{
    /// <summary>
    /// Provides gaze-based teleportation for VR locomotion.
    /// </summary>
    [AddComponentMenu("HUIX/Phone VR/Teleporter")]
    public class HUIXTeleporter : MonoBehaviour
    {
        #region Serialized Fields
        [Header("=== HUIX Teleporter ===")]
        [Space(10)]
        
        [Header("Settings")]
        [SerializeField] private float _maxDistance = 20f;
        [SerializeField] private LayerMask _teleportableLayers = 1;
        [SerializeField] private float _arcHeight = 2f;
        [SerializeField] private int _arcSegments = 30;
        
        [Header("Visual")]
        [SerializeField] private bool _showTeleportArc = true;
        [SerializeField] private Color _validColor = new Color(0f, 1f, 0.5f, 0.8f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private GameObject _targetIndicatorPrefab;
        
        [Header("Transition")]
        [SerializeField] private TeleportTransition _transition = TeleportTransition.Instant;
        [SerializeField] private float _transitionDuration = 0.3f;
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Constraints")]
        [SerializeField] private float _minTeleportDistance = 0.5f;
        [SerializeField] private float _maxSlopeAngle = 45f;
        #endregion

        #region Private Fields
        private HUIXVRRig _vrRig;
        private Camera _vrCamera;
        private LineRenderer _arcRenderer;
        private GameObject _targetIndicator;
        private Vector3 _targetPosition;
        private bool _isValidTarget;
        private bool _isAiming;
        private bool _isTeleporting;
        #endregion

        #region Enums
        public enum TeleportTransition
        {
            Instant,
            Fade,
            Dash
        }
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (_isTeleporting) return;

            UpdateAiming();
        }

        private void OnEnable()
        {
            HUIXInputManager.OnTriggerDown += StartAiming;
            HUIXInputManager.OnTriggerUp += ExecuteTeleport;
        }

        private void OnDisable()
        {
            HUIXInputManager.OnTriggerDown -= StartAiming;
            HUIXInputManager.OnTriggerUp -= ExecuteTeleport;
            
            StopAiming();
        }
        #endregion

        #region Initialization
        private void Initialize()
        {
            _vrRig = FindObjectOfType<HUIXVRRig>();
            
            HUIXVRCamera vrCam = FindObjectOfType<HUIXVRCamera>();
            if (vrCam != null)
            {
                _vrCamera = vrCam.GetComponent<Camera>();
            }
            else
            {
                _vrCamera = Camera.main;
            }

            // Create arc renderer
            if (_showTeleportArc)
            {
                CreateArcRenderer();
            }

            // Create target indicator
            CreateTargetIndicator();
        }

        private void CreateArcRenderer()
        {
            GameObject arcObj = new GameObject("Teleport Arc");
            arcObj.transform.SetParent(transform);
            
            _arcRenderer = arcObj.AddComponent<LineRenderer>();
            _arcRenderer.startWidth = 0.02f;
            _arcRenderer.endWidth = 0.02f;
            _arcRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _arcRenderer.positionCount = _arcSegments;
            _arcRenderer.enabled = false;
        }

        private void CreateTargetIndicator()
        {
            if (_targetIndicatorPrefab != null)
            {
                _targetIndicator = Instantiate(_targetIndicatorPrefab);
            }
            else
            {
                // Create default target indicator
                _targetIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _targetIndicator.name = "Teleport Target";
                _targetIndicator.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
                
                Destroy(_targetIndicator.GetComponent<Collider>());
                
                Material mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = _validColor;
                _targetIndicator.GetComponent<Renderer>().material = mat;
            }

            _targetIndicator.SetActive(false);
        }
        #endregion

        #region Aiming
        private void StartAiming()
        {
            _isAiming = true;
            
            if (_arcRenderer != null)
            {
                _arcRenderer.enabled = true;
            }
        }

        private void StopAiming()
        {
            _isAiming = false;
            
            if (_arcRenderer != null)
            {
                _arcRenderer.enabled = false;
            }

            if (_targetIndicator != null)
            {
                _targetIndicator.SetActive(false);
            }
        }

        private void UpdateAiming()
        {
            if (!_isAiming || _vrCamera == null) return;

            // Cast teleport arc
            Vector3 startPos = _vrCamera.transform.position;
            Vector3 forward = _vrCamera.transform.forward;
            
            _isValidTarget = CastTeleportArc(startPos, forward, out _targetPosition);

            // Update visuals
            UpdateArcVisual(startPos, forward);
            UpdateTargetIndicator();
        }

        private bool CastTeleportArc(Vector3 start, Vector3 direction, out Vector3 hitPosition)
        {
            hitPosition = Vector3.zero;

            // Simple raycast for now (arc calculation can be added)
            Vector3 velocity = direction * _maxDistance;
            Vector3 gravity = Physics.gravity;
            
            float timeStep = 1f / _arcSegments;
            Vector3 currentPos = start;
            Vector3 currentVelocity = velocity;

            for (int i = 0; i < _arcSegments * 2; i++)
            {
                Vector3 nextPos = currentPos + currentVelocity * timeStep + 0.5f * gravity * timeStep * timeStep;
                currentVelocity += gravity * timeStep;

                // Raycast between points
                if (Physics.Raycast(currentPos, nextPos - currentPos, out RaycastHit hit, (nextPos - currentPos).magnitude, _teleportableLayers))
                {
                    hitPosition = hit.point;

                    // Check slope
                    float angle = Vector3.Angle(hit.normal, Vector3.up);
                    if (angle > _maxSlopeAngle)
                    {
                        return false;
                    }

                    // Check minimum distance
                    float distance = Vector3.Distance(start, hitPosition);
                    if (distance < _minTeleportDistance)
                    {
                        return false;
                    }

                    return true;
                }

                currentPos = nextPos;
            }

            return false;
        }

        private void UpdateArcVisual(Vector3 start, Vector3 direction)
        {
            if (_arcRenderer == null) return;

            Vector3 velocity = direction * _maxDistance * 0.5f;
            Vector3 gravity = Physics.gravity;
            
            float timeStep = 1f / _arcSegments;
            Vector3 currentPos = start;
            Vector3 currentVelocity = velocity;

            for (int i = 0; i < _arcSegments; i++)
            {
                _arcRenderer.SetPosition(i, currentPos);
                
                Vector3 nextPos = currentPos + currentVelocity * timeStep + 0.5f * gravity * timeStep * timeStep;
                currentVelocity += gravity * timeStep;
                currentPos = nextPos;
            }

            // Update color based on validity
            Color arcColor = _isValidTarget ? _validColor : _invalidColor;
            _arcRenderer.startColor = arcColor;
            _arcRenderer.endColor = arcColor;
        }

        private void UpdateTargetIndicator()
        {
            if (_targetIndicator == null) return;

            if (_isValidTarget)
            {
                _targetIndicator.SetActive(true);
                _targetIndicator.transform.position = _targetPosition + Vector3.up * 0.01f;
                
                // Rotate indicator to match surface
                // _targetIndicator.transform.rotation = Quaternion.FromToRotation(Vector3.up, _targetNormal);
            }
            else
            {
                _targetIndicator.SetActive(false);
            }
        }
        #endregion

        #region Teleportation
        private void ExecuteTeleport()
        {
            if (!_isAiming || !_isValidTarget || _isTeleporting)
            {
                StopAiming();
                return;
            }

            StopAiming();
            StartCoroutine(TeleportCoroutine());
        }

        private IEnumerator TeleportCoroutine()
        {
            _isTeleporting = true;

            Transform rigTransform = _vrRig != null ? _vrRig.transform : transform;
            Vector3 startPosition = rigTransform.position;
            
            // Calculate final position (maintain height offset)
            Vector3 cameraOffset = _vrCamera != null ? _vrCamera.transform.position - rigTransform.position : Vector3.zero;
            cameraOffset.y = 0; // Keep horizontal offset only
            Vector3 finalPosition = _targetPosition - cameraOffset;

            switch (_transition)
            {
                case TeleportTransition.Instant:
                    rigTransform.position = finalPosition;
                    break;

                case TeleportTransition.Fade:
                    // Fade out
                    yield return StartCoroutine(FadeScreen(1f));
                    rigTransform.position = finalPosition;
                    // Fade in
                    yield return StartCoroutine(FadeScreen(0f));
                    break;

                case TeleportTransition.Dash:
                    float elapsed = 0f;
                    while (elapsed < _transitionDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = _transitionCurve.Evaluate(elapsed / _transitionDuration);
                        rigTransform.position = Vector3.Lerp(startPosition, finalPosition, t);
                        yield return null;
                    }
                    rigTransform.position = finalPosition;
                    break;
            }

            _isTeleporting = false;
        }

        private IEnumerator FadeScreen(float targetAlpha)
        {
            // Simple fade implementation - would use a full-screen overlay in practice
            float duration = _transitionDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Teleport to a specific position instantly
        /// </summary>
        public void TeleportTo(Vector3 position)
        {
            Transform rigTransform = _vrRig != null ? _vrRig.transform : transform;
            rigTransform.position = position;
        }

        /// <summary>
        /// Check if a position is valid for teleportation
        /// </summary>
        public bool IsValidTeleportTarget(Vector3 position)
        {
            if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 2f, _teleportableLayers))
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                return angle <= _maxSlopeAngle;
            }
            return false;
        }
        #endregion
    }
}

