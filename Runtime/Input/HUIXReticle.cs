/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Reticle - Visual gaze cursor for VR interaction
 */

using UnityEngine;

namespace HUIX.PhoneVR
{
    /// <summary>
    /// Visual reticle (crosshair) for gaze-based interaction.
    /// Shows where the user is looking and provides feedback.
    /// </summary>
    [AddComponentMenu("HUIX/Phone VR/Reticle")]
    public class HUIXReticle : MonoBehaviour
    {
        #region Serialized Fields
        [Header("=== HUIX Reticle ===")]
        [Space(10)]
        
        [Header("Appearance")]
        [SerializeField] private ReticleStyle _style = ReticleStyle.Dot;
        [SerializeField] private Color _normalColor = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private Color _hoverColor = new Color(0f, 0.8f, 1f, 1f);
        [SerializeField] private Color _selectColor = new Color(0f, 1f, 0.5f, 1f);
        
        [Header("Size")]
        [SerializeField] private float _baseSize = 0.02f;
        [SerializeField] private float _hoverSizeMultiplier = 1.5f;
        [SerializeField] private bool _scaleWithDistance = true;
        [SerializeField] private float _minScale = 0.5f;
        [SerializeField] private float _maxScale = 3f;
        
        [Header("Animation")]
        [SerializeField] private float _animationSpeed = 8f;
        [SerializeField] private bool _rotateReticle = true;
        [SerializeField] private float _rotationSpeed = 30f;
        
        [Header("Dwell Progress")]
        [SerializeField] private bool _showDwellProgress = true;
        [SerializeField] private Color _dwellColor = new Color(0f, 0.8f, 1f, 0.5f);
        
        [Header("3D Mode")]
        [SerializeField] private bool _use3DReticle = false;
        [SerializeField] private GameObject _reticle3DPrefab;
        #endregion

        #region Private Fields
        private GameObject _reticleObject;
        private MeshRenderer _meshRenderer;
        private Material _reticleMaterial;
        private float _currentSize;
        private float _targetSize;
        private Color _currentColor;
        private Color _targetColor;
        private float _dwellProgress;
        private bool _isHovering;
        private Camera _vrCamera;
        
        // Ring reticle specific
        private LineRenderer _ringRenderer;
        private LineRenderer _dwellRingRenderer;
        #endregion

        #region Enums
        public enum ReticleStyle
        {
            Dot,
            Ring,
            Crosshair,
            Custom3D
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            CreateReticle();
        }

        private void Start()
        {
            _vrCamera = Camera.main;
            
            // Try to get VR camera
            HUIXVRCamera vrCam = FindObjectOfType<HUIXVRCamera>();
            if (vrCam != null)
            {
                _vrCamera = vrCam.GetComponent<Camera>();
            }
        }

        private void Update()
        {
            AnimateReticle();
            
            if (_rotateReticle && _reticleObject != null)
            {
                _reticleObject.transform.Rotate(Vector3.forward, _rotationSpeed * Time.deltaTime);
            }
        }
        #endregion

        #region Reticle Creation
        private void CreateReticle()
        {
            switch (_style)
            {
                case ReticleStyle.Dot:
                    CreateDotReticle();
                    break;
                case ReticleStyle.Ring:
                    CreateRingReticle();
                    break;
                case ReticleStyle.Crosshair:
                    CreateCrosshairReticle();
                    break;
                case ReticleStyle.Custom3D:
                    CreateCustom3DReticle();
                    break;
            }

            _currentSize = _baseSize;
            _targetSize = _baseSize;
            _currentColor = _normalColor;
            _targetColor = _normalColor;
        }

        private void CreateDotReticle()
        {
            _reticleObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _reticleObject.name = "HUIX Reticle Dot";
            _reticleObject.transform.SetParent(transform);
            _reticleObject.transform.localPosition = Vector3.zero;
            _reticleObject.transform.localScale = Vector3.one * _baseSize;

            // Remove collider
            Destroy(_reticleObject.GetComponent<Collider>());

            // Setup material
            _meshRenderer = _reticleObject.GetComponent<MeshRenderer>();
            _reticleMaterial = new Material(Shader.Find("Sprites/Default"));
            _reticleMaterial.color = _normalColor;
            _meshRenderer.material = _reticleMaterial;

            // Create circular texture
            Texture2D circleTexture = CreateCircleTexture(64);
            _reticleMaterial.mainTexture = circleTexture;
        }

        private void CreateRingReticle()
        {
            _reticleObject = new GameObject("HUIX Reticle Ring");
            _reticleObject.transform.SetParent(transform);
            _reticleObject.transform.localPosition = Vector3.zero;

            // Main ring
            _ringRenderer = _reticleObject.AddComponent<LineRenderer>();
            ConfigureRingRenderer(_ringRenderer, _normalColor, _baseSize);

            // Dwell progress ring
            if (_showDwellProgress)
            {
                GameObject dwellObj = new GameObject("Dwell Ring");
                dwellObj.transform.SetParent(_reticleObject.transform);
                dwellObj.transform.localPosition = Vector3.zero;
                
                _dwellRingRenderer = dwellObj.AddComponent<LineRenderer>();
                ConfigureRingRenderer(_dwellRingRenderer, _dwellColor, _baseSize * 1.2f);
            }
        }

        private void ConfigureRingRenderer(LineRenderer renderer, Color color, float radius)
        {
            renderer.useWorldSpace = false;
            renderer.loop = true;
            renderer.startWidth = 0.002f;
            renderer.endWidth = 0.002f;
            renderer.startColor = color;
            renderer.endColor = color;
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            // Draw circle
            int segments = 32;
            renderer.positionCount = segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                renderer.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        private void CreateCrosshairReticle()
        {
            _reticleObject = new GameObject("HUIX Reticle Crosshair");
            _reticleObject.transform.SetParent(transform);
            _reticleObject.transform.localPosition = Vector3.zero;

            // Horizontal line
            CreateLine("Horizontal", Vector3.left * _baseSize, Vector3.right * _baseSize);
            
            // Vertical line
            CreateLine("Vertical", Vector3.down * _baseSize, Vector3.up * _baseSize);
            
            // Center dot
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dot.name = "Center Dot";
            dot.transform.SetParent(_reticleObject.transform);
            dot.transform.localPosition = Vector3.zero;
            dot.transform.localScale = Vector3.one * _baseSize * 0.3f;
            Destroy(dot.GetComponent<Collider>());
            
            _meshRenderer = dot.GetComponent<MeshRenderer>();
            _reticleMaterial = new Material(Shader.Find("Sprites/Default"));
            _reticleMaterial.color = _normalColor;
            _reticleMaterial.mainTexture = CreateCircleTexture(32);
            _meshRenderer.material = _reticleMaterial;
        }

        private void CreateLine(string name, Vector3 start, Vector3 end)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(_reticleObject.transform);
            lineObj.transform.localPosition = Vector3.zero;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.002f;
            line.endWidth = 0.002f;
            line.startColor = _normalColor;
            line.endColor = _normalColor;
            line.material = new Material(Shader.Find("Sprites/Default"));
        }

        private void CreateCustom3DReticle()
        {
            if (_reticle3DPrefab != null)
            {
                _reticleObject = Instantiate(_reticle3DPrefab, transform);
                _reticleObject.name = "HUIX Custom Reticle";
            }
            else
            {
                // Fallback to dot
                CreateDotReticle();
            }
        }

        private Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - dist);
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            texture.Apply();
            return texture;
        }
        #endregion

        #region Animation
        private void AnimateReticle()
        {
            // Animate size
            _currentSize = Mathf.Lerp(_currentSize, _targetSize, Time.deltaTime * _animationSpeed);
            
            // Animate color
            _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * _animationSpeed);

            // Apply
            if (_reticleObject != null)
            {
                _reticleObject.transform.localScale = Vector3.one * _currentSize;
            }

            if (_reticleMaterial != null)
            {
                _reticleMaterial.color = _currentColor;
            }

            // Update dwell ring
            if (_showDwellProgress && _dwellRingRenderer != null)
            {
                UpdateDwellRing();
            }
        }

        private void UpdateDwellRing()
        {
            HUIXInputManager inputManager = FindObjectOfType<HUIXInputManager>();
            if (inputManager != null)
            {
                _dwellProgress = inputManager.GazeDwellProgress;
            }

            // Show portion of ring based on progress
            int totalSegments = _dwellRingRenderer.positionCount;
            int activeSegments = Mathf.RoundToInt(totalSegments * _dwellProgress);

            for (int i = 0; i < totalSegments; i++)
            {
                // This is a simplified approach - a real implementation would 
                // dynamically modify the ring segments
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Update reticle based on gaze raycast
        /// </summary>
        public void UpdateReticle(Ray gazeRay, RaycastHit hit, bool isHovering)
        {
            _isHovering = isHovering;
            
            if (isHovering)
            {
                _targetColor = _hoverColor;
                _targetSize = _baseSize * _hoverSizeMultiplier;
            }
            else
            {
                _targetColor = _normalColor;
                _targetSize = _baseSize;
            }
        }

        /// <summary>
        /// Set reticle position at hit point
        /// </summary>
        public void SetPosition(Vector3 position, Vector3 normal)
        {
            transform.position = position;
            
            // Face the camera
            if (_vrCamera != null)
            {
                transform.LookAt(_vrCamera.transform);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(-normal);
            }

            // Scale with distance
            if (_scaleWithDistance && _vrCamera != null)
            {
                float distance = Vector3.Distance(_vrCamera.transform.position, position);
                float scale = Mathf.Clamp(distance * 0.1f, _minScale, _maxScale);
                _targetSize = _baseSize * scale * (_isHovering ? _hoverSizeMultiplier : 1f);
            }
        }

        /// <summary>
        /// Set reticle to default position in front of camera
        /// </summary>
        public void SetDefaultPosition(float distance)
        {
            if (_vrCamera != null)
            {
                transform.position = _vrCamera.transform.position + _vrCamera.transform.forward * distance;
                transform.LookAt(_vrCamera.transform);
            }

            if (_scaleWithDistance)
            {
                float scale = Mathf.Clamp(distance * 0.1f, _minScale, _maxScale);
                _targetSize = _baseSize * scale;
            }
        }

        /// <summary>
        /// Flash the reticle (for selection feedback)
        /// </summary>
        public void Flash()
        {
            _currentColor = _selectColor;
            _currentSize = _baseSize * 2f;
        }

        /// <summary>
        /// Set reticle style
        /// </summary>
        public void SetStyle(ReticleStyle style)
        {
            if (_reticleObject != null)
            {
                Destroy(_reticleObject);
            }

            _style = style;
            CreateReticle();
        }

        /// <summary>
        /// Show or hide the reticle
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_reticleObject != null)
            {
                _reticleObject.SetActive(visible);
            }
        }
        #endregion
    }
}

