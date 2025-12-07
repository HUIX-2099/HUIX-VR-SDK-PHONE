/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Demo Scene Manager - Showcases SDK features
 */

using UnityEngine;
using HUIX.PhoneVR;
using HUIX.PhoneVR.Core;
using HUIX.PhoneVR.UI;

namespace HUIX.PhoneVR.Samples
{
    /// <summary>
    /// Demo scene that showcases all HUIX Phone VR SDK features.
    /// </summary>
    public class DemoSceneManager : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private bool _autoCreateEnvironment = true;
        [SerializeField] private bool _showInstructions = true;
        
        [Header("Colors")]
        [SerializeField] private Color _skyColor = new Color(0.1f, 0.1f, 0.2f);
        [SerializeField] private Color _groundColor = new Color(0.2f, 0.2f, 0.25f);
        [SerializeField] private Color _accentColor = new Color(0.2f, 0.6f, 1f);

        private HUIXVRManager _vrManager;
        private HUIXVRRig _vrRig;

        private void Start()
        {
            SetupVR();
            
            if (_autoCreateEnvironment)
            {
                CreateDemoEnvironment();
            }

            if (_showInstructions)
            {
                CreateInstructionsUI();
            }
        }

        private void SetupVR()
        {
            // Find or create VR components
            _vrManager = FindObjectOfType<HUIXVRManager>();
            if (_vrManager == null)
            {
                GameObject managerObj = new GameObject("HUIX VR Manager");
                _vrManager = managerObj.AddComponent<HUIXVRManager>();
            }

            _vrRig = FindObjectOfType<HUIXVRRig>();
            if (_vrRig == null)
            {
                GameObject rigObj = new GameObject("HUIX VR Rig");
                _vrRig = rigObj.AddComponent<HUIXVRRig>();
            }

            // Set sky color
            Camera.main.backgroundColor = _skyColor;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
        }

        private void CreateDemoEnvironment()
        {
            // Create ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);
            
            Material groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = _groundColor;
            ground.GetComponent<Renderer>().material = groundMat;

            // Create grid pattern on ground
            CreateGridLines();

            // Create interactive objects
            CreateInteractiveObjects();

            // Create floating info panels
            CreateInfoPanels();

            // Create skybox elements
            CreateSkyboxElements();
        }

        private void CreateGridLines()
        {
            GameObject gridParent = new GameObject("Grid Lines");
            
            for (int i = -5; i <= 5; i++)
            {
                // X lines
                CreateLine(gridParent.transform, 
                    new Vector3(-50, 0.01f, i * 10), 
                    new Vector3(50, 0.01f, i * 10),
                    new Color(0.3f, 0.3f, 0.4f, 0.5f));
                
                // Z lines
                CreateLine(gridParent.transform, 
                    new Vector3(i * 10, 0.01f, -50), 
                    new Vector3(i * 10, 0.01f, 50),
                    new Color(0.3f, 0.3f, 0.4f, 0.5f));
            }
        }

        private void CreateLine(Transform parent, Vector3 start, Vector3 end, Color color)
        {
            GameObject lineObj = new GameObject("Line");
            lineObj.transform.SetParent(parent);
            
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.02f;
            line.endWidth = 0.02f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
        }

        private void CreateInteractiveObjects()
        {
            // Create interactive cubes in a circle
            int numCubes = 8;
            float radius = 5f;
            float height = 1.2f;

            for (int i = 0; i < numCubes; i++)
            {
                float angle = (float)i / numCubes * Mathf.PI * 2f;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    height,
                    Mathf.Sin(angle) * radius
                );

                CreateInteractiveCube(position, i);
            }

            // Create central interactive sphere
            CreateInteractiveSphere(new Vector3(0, 1.5f, 3f));
        }

        private void CreateInteractiveCube(Vector3 position, int index)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Interactive Cube {index}";
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * 0.5f;
            
            // Add interactable component
            DemoInteractable interactable = cube.AddComponent<DemoInteractable>();
            interactable.SetColor(Color.HSVToRGB((float)index / 8f, 0.7f, 0.9f));
            
            // Add collider for raycasting
            if (cube.GetComponent<Collider>() == null)
            {
                cube.AddComponent<BoxCollider>();
            }
        }

        private void CreateInteractiveSphere(Vector3 position)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Interactive Sphere";
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * 0.8f;
            
            DemoInteractable interactable = sphere.AddComponent<DemoInteractable>();
            interactable.SetColor(_accentColor);
            interactable.SetAsMainTarget();
        }

        private void CreateInfoPanels()
        {
            // Welcome panel
            CreateTextPanel(
                new Vector3(0, 2.5f, 4f),
                "HUIX Phone VR SDK",
                "Welcome to the Demo Scene!\n\nLook around by moving your phone.\nGaze at objects to interact.",
                2f, 1f
            );

            // Controls panel
            CreateTextPanel(
                new Vector3(-3f, 1.5f, 3f),
                "Controls",
                "• Look: Move phone\n• Select: Tap screen\n• Recenter: Double tap",
                1.5f, 1f
            );

            // Features panel
            CreateTextPanel(
                new Vector3(3f, 1.5f, 3f),
                "Features",
                "• Stereoscopic 3D\n• Head Tracking\n• Lens Correction\n• Gaze Input",
                1.5f, 1f
            );
        }

        private void CreateTextPanel(Vector3 position, string title, string content, float width, float height)
        {
            GameObject panel = new GameObject($"Panel - {title}");
            panel.transform.position = position;
            
            // Background
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "Background";
            bg.transform.SetParent(panel.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(width, height, 1);
            
            Destroy(bg.GetComponent<Collider>());
            
            Material bgMat = new Material(Shader.Find("Sprites/Default"));
            bgMat.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            bg.GetComponent<Renderer>().material = bgMat;

            // Use TextMesh for 3D text (legacy but simple)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform);
            titleObj.transform.localPosition = new Vector3(0, height * 0.35f, -0.01f);
            
            TextMesh titleText = titleObj.AddComponent<TextMesh>();
            titleText.text = title;
            titleText.fontSize = 50;
            titleText.characterSize = 0.015f;
            titleText.anchor = TextAnchor.MiddleCenter;
            titleText.alignment = TextAlignment.Center;
            titleText.color = _accentColor;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(panel.transform);
            contentObj.transform.localPosition = new Vector3(0, -0.05f, -0.01f);
            
            TextMesh contentText = contentObj.AddComponent<TextMesh>();
            contentText.text = content;
            contentText.fontSize = 40;
            contentText.characterSize = 0.012f;
            contentText.anchor = TextAnchor.UpperCenter;
            contentText.alignment = TextAlignment.Center;
            contentText.color = Color.white;

            // Face camera
            panel.AddComponent<FaceCamera>();
        }

        private void CreateSkyboxElements()
        {
            // Create distant floating particles/stars
            GameObject starsParent = new GameObject("Stars");
            
            for (int i = 0; i < 100; i++)
            {
                GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = $"Star {i}";
                star.transform.SetParent(starsParent.transform);
                
                // Random position on a sphere
                Vector3 randomDir = Random.onUnitSphere;
                star.transform.position = randomDir * Random.Range(30f, 50f);
                
                float size = Random.Range(0.05f, 0.2f);
                star.transform.localScale = Vector3.one * size;
                
                Destroy(star.GetComponent<Collider>());
                
                Material starMat = new Material(Shader.Find("Sprites/Default"));
                starMat.color = new Color(1f, 1f, 1f, Random.Range(0.3f, 1f));
                star.GetComponent<Renderer>().material = starMat;
            }
        }

        private void CreateInstructionsUI()
        {
            // Create on-screen instructions that appear at start
            // This would use Unity UI in a real implementation
        }

        private void Update()
        {
            // Demo-specific updates
            
            // R key to recenter
            if (Input.GetKeyDown(KeyCode.R))
            {
                _vrManager?.Recenter();
            }

            // V key to toggle VR mode
            if (Input.GetKeyDown(KeyCode.V))
            {
                _vrManager?.ToggleVRMode();
            }
        }
    }

    /// <summary>
    /// Demo interactable object
    /// </summary>
    public class DemoInteractable : MonoBehaviour, IHUIXInteractable
    {
        private Renderer _renderer;
        private Material _material;
        private Color _baseColor;
        private Color _hoverColor;
        private Vector3 _originalScale;
        private bool _isMainTarget;
        private float _rotationSpeed;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _material = _renderer.material;
            _baseColor = _material.color;
            _hoverColor = _baseColor * 1.5f;
            _originalScale = transform.localScale;
            _rotationSpeed = Random.Range(-30f, 30f);
        }

        private void Update()
        {
            // Gentle rotation
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
            
            if (_isMainTarget)
            {
                // Gentle bobbing
                float bob = Mathf.Sin(Time.time * 2f) * 0.1f;
                transform.position = new Vector3(transform.position.x, 1.5f + bob, transform.position.z);
            }
        }

        public void SetColor(Color color)
        {
            _baseColor = color;
            _hoverColor = color * 1.5f;
            if (_material != null)
            {
                _material.color = color;
            }
        }

        public void SetAsMainTarget()
        {
            _isMainTarget = true;
            transform.localScale = _originalScale * 1.5f;
            _originalScale = transform.localScale;
        }

        public void OnGazeEnter()
        {
            _material.color = _hoverColor;
            transform.localScale = _originalScale * 1.2f;
        }

        public void OnGazeExit()
        {
            _material.color = _baseColor;
            transform.localScale = _originalScale;
        }

        public void OnSelect()
        {
            // Visual feedback
            StartCoroutine(SelectAnimation());
            
            // Change to random color
            SetColor(Random.ColorHSV(0f, 1f, 0.7f, 0.9f, 0.8f, 1f));
            
            Debug.Log($"[Demo] Selected: {gameObject.name}");
        }

        private System.Collections.IEnumerator SelectAnimation()
        {
            Vector3 startScale = transform.localScale;
            Vector3 punchScale = startScale * 1.5f;
            
            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                if (t < 0.5f)
                {
                    transform.localScale = Vector3.Lerp(startScale, punchScale, t * 2f);
                }
                else
                {
                    transform.localScale = Vector3.Lerp(punchScale, _originalScale * 1.2f, (t - 0.5f) * 2f);
                }
                
                yield return null;
            }

            transform.localScale = _originalScale * 1.2f;
        }
    }

    /// <summary>
    /// Makes object always face the camera
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_camera != null)
            {
                transform.LookAt(transform.position + _camera.transform.forward);
            }
        }
    }
}

