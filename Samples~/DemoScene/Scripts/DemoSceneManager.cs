/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Demo Scene - Simple demo environment
 * For VR, just add CardboardVR to your camera!
 */

using UnityEngine;

namespace HUIX.PhoneVR.Samples
{
    /// <summary>
    /// Simple demo scene - creates a test environment.
    /// For VR mode, add the CardboardVR component to your camera!
    /// </summary>
    [AddComponentMenu("HUIX/Samples/Demo Scene")]
    public class DemoSceneManager : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private bool _autoCreateEnvironment = true;
        
        [Header("Colors")]
        [SerializeField] private Color _skyColor = new Color(0.05f, 0.05f, 0.1f);
        [SerializeField] private Color _groundColor = new Color(0.15f, 0.15f, 0.2f);
        [SerializeField] private Color _accentColor = new Color(0.2f, 0.6f, 1f);

        private void Start()
        {
            // Setup camera background
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = _skyColor;
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
            }
            
            if (_autoCreateEnvironment)
            {
                CreateDemoEnvironment();
            }
            
            Debug.Log("[HUIX Demo] Demo scene ready! Add CardboardVR to camera for VR mode.");
        }

        private void CreateDemoEnvironment()
        {
            CreateGround();
            CreateGridLines();
            CreateInteractiveObjects();
            CreateInfoPanels();
            CreateStars();
        }

        private void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);
            
            Renderer rend = ground.GetComponent<Renderer>();
            rend.material = CreateMaterial(_groundColor);
        }

        private void CreateGridLines()
        {
            GameObject gridParent = new GameObject("Grid Lines");
            Material lineMat = CreateMaterial(new Color(0.3f, 0.3f, 0.4f, 0.5f));
            
            for (int i = -5; i <= 5; i++)
            {
                CreateLine(gridParent.transform, 
                    new Vector3(-50, 0.01f, i * 10), 
                    new Vector3(50, 0.01f, i * 10), lineMat);
                
                CreateLine(gridParent.transform, 
                    new Vector3(i * 10, 0.01f, -50), 
                    new Vector3(i * 10, 0.01f, 50), lineMat);
            }
        }

        private void CreateLine(Transform parent, Vector3 start, Vector3 end, Material mat)
        {
            GameObject lineObj = new GameObject("Line");
            lineObj.transform.SetParent(parent);
            
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.02f;
            line.endWidth = 0.02f;
            line.material = mat;
            line.startColor = mat.color;
            line.endColor = mat.color;
        }

        private void CreateInteractiveObjects()
        {
            // Circle of cubes
            int numCubes = 8;
            float radius = 5f;

            for (int i = 0; i < numCubes; i++)
            {
                float angle = (float)i / numCubes * Mathf.PI * 2f;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    1.2f,
                    Mathf.Sin(angle) * radius
                );

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Cube {i}";
                cube.transform.position = position;
                cube.transform.localScale = Vector3.one * 0.5f;
                cube.GetComponent<Renderer>().material = CreateMaterial(
                    Color.HSVToRGB((float)i / 8f, 0.7f, 0.9f));
                cube.AddComponent<SpinObject>();
            }

            // Center sphere
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Center Sphere";
            sphere.transform.position = new Vector3(0, 1.5f, 3f);
            sphere.transform.localScale = Vector3.one * 0.8f;
            sphere.GetComponent<Renderer>().material = CreateMaterial(_accentColor);
            sphere.AddComponent<BobObject>();
        }

        private void CreateInfoPanels()
        {
            CreateTextPanel(new Vector3(0, 2.5f, 4f), "HUIX Phone VR", 
                "Add CardboardVR to camera for VR!");
            CreateTextPanel(new Vector3(-3f, 1.5f, 3f), "Controls", 
                "Double-tap: Recenter\nR: Recenter\nSpace: Toggle VR");
            CreateTextPanel(new Vector3(3f, 1.5f, 3f), "Features", 
                "Stereo 3D\nHead Tracking\nLens Correction");
        }

        private void CreateTextPanel(Vector3 position, string title, string content)
        {
            GameObject panel = new GameObject($"Panel - {title}");
            panel.transform.position = position;
            
            // Background quad
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BG";
            bg.transform.SetParent(panel.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(2f, 1f, 1);
            Destroy(bg.GetComponent<Collider>());
            bg.GetComponent<Renderer>().material = CreateMaterial(new Color(0.1f, 0.1f, 0.15f, 0.9f));

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform);
            titleObj.transform.localPosition = new Vector3(0, 0.35f, -0.01f);
            TextMesh titleText = titleObj.AddComponent<TextMesh>();
            titleText.text = title;
            titleText.fontSize = 50;
            titleText.characterSize = 0.015f;
            titleText.anchor = TextAnchor.MiddleCenter;
            titleText.color = _accentColor;

            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(panel.transform);
            contentObj.transform.localPosition = new Vector3(0, -0.05f, -0.01f);
            TextMesh contentText = contentObj.AddComponent<TextMesh>();
            contentText.text = content;
            contentText.fontSize = 40;
            contentText.characterSize = 0.01f;
            contentText.anchor = TextAnchor.UpperCenter;
            contentText.color = Color.white;

            panel.AddComponent<LookAtCamera>();
        }

        private void CreateStars()
        {
            GameObject starsParent = new GameObject("Stars");
            Material starMat = CreateMaterial(Color.white);
            
            for (int i = 0; i < 100; i++)
            {
                GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.transform.SetParent(starsParent.transform);
                star.transform.position = Random.onUnitSphere * Random.Range(30f, 50f);
                star.transform.localScale = Vector3.one * Random.Range(0.05f, 0.2f);
                Destroy(star.GetComponent<Collider>());
                star.GetComponent<Renderer>().material = starMat;
            }
        }

        private Material CreateMaterial(Color color)
        {
            // Use a shader that always exists
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            if (shader == null) shader = Shader.Find("VertexLit");
            
            Material mat;
            if (shader != null)
            {
                mat = new Material(shader);
            }
            else
            {
                // Absolute fallback
                mat = new Material(Shader.Find("Sprites/Default"));
            }
            
            mat.color = color;
            return mat;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }
    }

    // Simple helper components
    public class SpinObject : MonoBehaviour
    {
        private float _speed;
        void Start() { _speed = Random.Range(-50f, 50f); }
        void Update() { transform.Rotate(Vector3.up, _speed * Time.deltaTime); }
    }

    public class BobObject : MonoBehaviour
    {
        private Vector3 _startPos;
        void Start() { _startPos = transform.position; }
        void Update() { 
            transform.position = _startPos + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.1f; 
        }
    }

    public class LookAtCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            Camera cam = Camera.main ?? (Camera.allCamerasCount > 0 ? Camera.allCameras[0] : null);
            if (cam != null)
            {
                transform.LookAt(transform.position + cam.transform.forward);
            }
        }
    }
}
