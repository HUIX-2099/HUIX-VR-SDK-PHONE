/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Demo Scene Manager - Auto VR Cardboard Setup
 * Just add to scene and it works!
 */

using UnityEngine;
using System.Collections;

namespace HUIX.PhoneVR.Samples
{
    /// <summary>
    /// Demo scene with AUTO VR MODE - Google Cardboard style!
    /// Just attach to any GameObject and press Play.
    /// </summary>
    [AddComponentMenu("HUIX/Samples/Demo Scene Manager (Auto VR)")]
    public class DemoSceneManager : MonoBehaviour
    {
        [Header("=== AUTO VR SETTINGS ===")]
        [SerializeField] private bool _enableVRMode = true;
        [SerializeField] private float _eyeSeparation = 0.064f; // 64mm IPD
        [SerializeField] private bool _enableLensDistortion = true;
        [SerializeField] private bool _enableHeadTracking = true;
        
        [Header("Demo Environment")]
        [SerializeField] private bool _autoCreateEnvironment = true;
        
        [Header("Colors")]
        [SerializeField] private Color _skyColor = new Color(0.05f, 0.05f, 0.1f);
        [SerializeField] private Color _groundColor = new Color(0.15f, 0.15f, 0.2f);
        [SerializeField] private Color _accentColor = new Color(0.2f, 0.6f, 1f);

        // VR Components
        private Camera _leftEye;
        private Camera _rightEye;
        private Camera _mainCamera;
        private Transform _vrRig;
        private Material _distortionMaterial;
        private RenderTexture _leftEyeTexture;
        private RenderTexture _rightEyeTexture;

        private void Start()
        {
            if (_enableVRMode)
            {
                SetupVRMode();
            }
            else
            {
                SetupNormalMode();
            }
            
            if (_autoCreateEnvironment)
            {
                CreateDemoEnvironment();
            }

            Debug.Log("[HUIX VR] Demo ready! VR Mode: " + (_enableVRMode ? "ON" : "OFF"));
        }

        #region VR Setup
        
        private void SetupVRMode()
        {
            // Create VR Rig hierarchy
            _vrRig = new GameObject("HUIX VR Rig").transform;
            _vrRig.position = new Vector3(0, 1.6f, 0); // Eye height
            
            // Disable original camera
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _mainCamera.gameObject.SetActive(false);
            }
            
            // Create stereo cameras
            CreateStereoCameras();
            
            // Setup lens distortion
            if (_enableLensDistortion)
            {
                SetupLensDistortion();
            }
            
            // Enable head tracking
            if (_enableHeadTracking)
            {
                _vrRig.gameObject.AddComponent<SimpleHeadTracker>();
            }
        }
        
        private void SetupNormalMode()
        {
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _mainCamera.backgroundColor = _skyColor;
                _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            }
        }
        
        private void CreateStereoCameras()
        {
            // Camera holder (rotates with head)
            GameObject cameraHolder = new GameObject("Camera Holder");
            cameraHolder.transform.SetParent(_vrRig);
            cameraHolder.transform.localPosition = Vector3.zero;
            
            // Left Eye
            GameObject leftEyeObj = new GameObject("Left Eye");
            leftEyeObj.transform.SetParent(cameraHolder.transform);
            leftEyeObj.transform.localPosition = new Vector3(-_eyeSeparation / 2f, 0, 0);
            _leftEye = leftEyeObj.AddComponent<Camera>();
            _leftEye.backgroundColor = _skyColor;
            _leftEye.clearFlags = CameraClearFlags.SolidColor;
            _leftEye.fieldOfView = 90f;
            _leftEye.nearClipPlane = 0.1f;
            _leftEye.rect = new Rect(0, 0, 0.5f, 1); // Left half of screen
            _leftEye.depth = 0;
            
            // Right Eye
            GameObject rightEyeObj = new GameObject("Right Eye");
            rightEyeObj.transform.SetParent(cameraHolder.transform);
            rightEyeObj.transform.localPosition = new Vector3(_eyeSeparation / 2f, 0, 0);
            _rightEye = rightEyeObj.AddComponent<Camera>();
            _rightEye.backgroundColor = _skyColor;
            _rightEye.clearFlags = CameraClearFlags.SolidColor;
            _rightEye.fieldOfView = 90f;
            _rightEye.nearClipPlane = 0.1f;
            _rightEye.rect = new Rect(0.5f, 0, 0.5f, 1); // Right half of screen
            _rightEye.depth = 0;
            
            // Add audio listener to left eye only
            leftEyeObj.AddComponent<AudioListener>();
        }
        
        private void SetupLensDistortion()
        {
            // Create render textures for each eye
            int width = Screen.width / 2;
            int height = Screen.height;
            
            _leftEyeTexture = new RenderTexture(width, height, 24);
            _rightEyeTexture = new RenderTexture(width, height, 24);
            
            _leftEye.targetTexture = _leftEyeTexture;
            _rightEye.targetTexture = _rightEyeTexture;
            
            // Create distortion material
            _distortionMaterial = new Material(Shader.Find("Hidden/HUIX/LensDistortion"));
            if (_distortionMaterial.shader == null || !_distortionMaterial.shader.isSupported)
            {
                // Fallback - create simple unlit shader
                _distortionMaterial = CreateFallbackDistortionMaterial();
            }
            
            // Create display camera
            GameObject displayCamObj = new GameObject("Display Camera");
            displayCamObj.transform.SetParent(_vrRig);
            Camera displayCam = displayCamObj.AddComponent<Camera>();
            displayCam.clearFlags = CameraClearFlags.SolidColor;
            displayCam.backgroundColor = Color.black;
            displayCam.cullingMask = 0;
            displayCam.depth = 10;
            displayCam.gameObject.AddComponent<VRDisplayRenderer>().Initialize(
                _leftEyeTexture, _rightEyeTexture, _distortionMaterial);
        }
        
        private Material CreateFallbackDistortionMaterial()
        {
            // Simple barrel distortion shader
            string shaderCode = @"
Shader ""Hidden/HUIX/LensDistortionFallback""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _Distortion (""Distortion"", Float) = 0.2
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            
            sampler2D _MainTex;
            float _Distortion;
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 center = float2(0.5, 0.5);
                float2 delta = uv - center;
                float dist = length(delta);
                float distortion = 1.0 + _Distortion * dist * dist;
                float2 distortedUV = center + delta * distortion;
                
                if (distortedUV.x < 0 || distortedUV.x > 1 || distortedUV.y < 0 || distortedUV.y > 1)
                    return float4(0, 0, 0, 1);
                    
                return tex2D(_MainTex, distortedUV);
            }
            ENDCG
        }
    }
}";
            // Can't compile shader at runtime, use unlit as fallback
            Material mat = new Material(Shader.Find("Unlit/Texture"));
            return mat;
        }
        
        #endregion

        #region Demo Environment

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

            CreateGridLines();
            CreateInteractiveObjects();
            CreateInfoPanels();
            CreateSkyboxElements();
        }

        private void CreateGridLines()
        {
            GameObject gridParent = new GameObject("Grid Lines");
            
            for (int i = -5; i <= 5; i++)
            {
                CreateLine(gridParent.transform, 
                    new Vector3(-50, 0.01f, i * 10), 
                    new Vector3(50, 0.01f, i * 10),
                    new Color(0.3f, 0.3f, 0.4f, 0.5f));
                
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

            CreateInteractiveSphere(new Vector3(0, 1.5f, 3f));
        }

        private void CreateInteractiveCube(Vector3 position, int index)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Interactive Cube {index}";
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * 0.5f;
            
            DemoInteractable interactable = cube.AddComponent<DemoInteractable>();
            interactable.SetColor(Color.HSVToRGB((float)index / 8f, 0.7f, 0.9f));
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
            CreateTextPanel(
                new Vector3(0, 2.5f, 4f),
                "HUIX Phone VR",
                "Welcome to VR!\n\nMove your phone to look around.",
                2f, 1f
            );

            CreateTextPanel(
                new Vector3(-3f, 1.5f, 3f),
                "Controls",
                "• Look: Move phone\n• Tap: Interact",
                1.5f, 1f
            );

            CreateTextPanel(
                new Vector3(3f, 1.5f, 3f),
                "Features",
                "• Stereo 3D\n• Head Tracking\n• Lens Correction",
                1.5f, 1f
            );
        }

        private void CreateTextPanel(Vector3 position, string title, string content, float width, float height)
        {
            GameObject panel = new GameObject($"Panel - {title}");
            panel.transform.position = position;
            
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "Background";
            bg.transform.SetParent(panel.transform);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(width, height, 1);
            
            Destroy(bg.GetComponent<Collider>());
            
            Material bgMat = new Material(Shader.Find("Sprites/Default"));
            bgMat.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            bg.GetComponent<Renderer>().material = bgMat;

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

            panel.AddComponent<FaceCamera>();
        }

        private void CreateSkyboxElements()
        {
            GameObject starsParent = new GameObject("Stars");
            
            for (int i = 0; i < 100; i++)
            {
                GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = $"Star {i}";
                star.transform.SetParent(starsParent.transform);
                
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

        #endregion

        private void Update()
        {
            // Tap to recenter
            if (Input.touchCount > 0 && Input.GetTouch(0).tapCount == 2)
            {
                Recenter();
            }
            
            // Keyboard controls for testing
            if (Input.GetKeyDown(KeyCode.R))
            {
                Recenter();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }
        
        private void Recenter()
        {
            if (_vrRig != null)
            {
                SimpleHeadTracker tracker = _vrRig.GetComponent<SimpleHeadTracker>();
                if (tracker != null)
                {
                    tracker.Recenter();
                }
            }
            Debug.Log("[HUIX VR] View recentered!");
        }

        private void OnDestroy()
        {
            if (_leftEyeTexture != null) _leftEyeTexture.Release();
            if (_rightEyeTexture != null) _rightEyeTexture.Release();
        }
    }

    /// <summary>
    /// Simple gyroscope head tracker
    /// </summary>
    public class SimpleHeadTracker : MonoBehaviour
    {
        private Quaternion _initialRotation;
        private Quaternion _gyroOffset;
        private bool _gyroEnabled;

        private void Start()
        {
            // Enable gyroscope
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                _gyroEnabled = true;
                _gyroOffset = Quaternion.identity;
                Debug.Log("[HUIX VR] Gyroscope enabled for head tracking");
            }
            else
            {
                Debug.LogWarning("[HUIX VR] Gyroscope not available - using mouse for testing");
            }
            
            _initialRotation = transform.rotation;
        }

        private void Update()
        {
            if (_gyroEnabled)
            {
                // Get gyro rotation and convert to Unity coordinates
                Quaternion gyro = Input.gyro.attitude;
                Quaternion rotation = new Quaternion(gyro.x, gyro.y, -gyro.z, -gyro.w);
                
                // Apply rotation with offset
                transform.localRotation = _gyroOffset * rotation * Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                // Mouse look for editor testing
                float mouseX = Input.GetAxis("Mouse X") * 2f;
                float mouseY = Input.GetAxis("Mouse Y") * 2f;
                
                if (Input.GetMouseButton(1)) // Right mouse button
                {
                    transform.Rotate(Vector3.up, mouseX, Space.World);
                    transform.Rotate(Vector3.right, -mouseY, Space.Self);
                }
            }
        }

        public void Recenter()
        {
            if (_gyroEnabled)
            {
                Quaternion gyro = Input.gyro.attitude;
                Quaternion currentRotation = new Quaternion(gyro.x, gyro.y, -gyro.z, -gyro.w);
                _gyroOffset = Quaternion.Inverse(currentRotation);
            }
            else
            {
                transform.rotation = _initialRotation;
            }
        }
    }

    /// <summary>
    /// Renders the stereo view with optional lens distortion
    /// </summary>
    public class VRDisplayRenderer : MonoBehaviour
    {
        private RenderTexture _leftTex;
        private RenderTexture _rightTex;
        private Material _material;

        public void Initialize(RenderTexture left, RenderTexture right, Material mat)
        {
            _leftTex = left;
            _rightTex = right;
            _material = mat;
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (_leftTex == null || _rightTex == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            // Create split screen effect
            RenderTexture temp = RenderTexture.GetTemporary(dest.width, dest.height);
            
            // Draw left eye
            Graphics.SetRenderTarget(temp);
            GL.Clear(true, true, Color.black);
            
            GL.PushMatrix();
            GL.LoadOrtho();
            
            // Left eye (left half)
            _material.mainTexture = _leftTex;
            DrawQuad(0, 0, 0.5f, 1);
            
            // Right eye (right half)
            _material.mainTexture = _rightTex;
            DrawQuad(0.5f, 0, 0.5f, 1);
            
            GL.PopMatrix();
            
            Graphics.Blit(temp, dest);
            RenderTexture.ReleaseTemporary(temp);
        }

        private void DrawQuad(float x, float y, float width, float height)
        {
            _material.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.TexCoord2(0, 0); GL.Vertex3(x, y, 0);
            GL.TexCoord2(1, 0); GL.Vertex3(x + width, y, 0);
            GL.TexCoord2(1, 1); GL.Vertex3(x + width, y + height, 0);
            GL.TexCoord2(0, 1); GL.Vertex3(x, y + height, 0);
            GL.End();
        }
    }

    /// <summary>
    /// Demo interactable object
    /// </summary>
    public class DemoInteractable : MonoBehaviour
    {
        private Renderer _renderer;
        private Material _material;
        private Color _baseColor;
        private Vector3 _originalScale;
        private bool _isMainTarget;
        private float _rotationSpeed;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _material = _renderer.material;
            _baseColor = _material.color;
            _originalScale = transform.localScale;
            _rotationSpeed = Random.Range(-30f, 30f);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
            
            if (_isMainTarget)
            {
                float bob = Mathf.Sin(Time.time * 2f) * 0.1f;
                transform.position = new Vector3(transform.position.x, 1.5f + bob, transform.position.z);
            }
        }

        public void SetColor(Color color)
        {
            _baseColor = color;
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
    }

    /// <summary>
    /// Makes object always face the camera
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null && Camera.allCameras.Length > 0)
            {
                cam = Camera.allCameras[0];
            }
            
            if (cam != null)
            {
                transform.LookAt(transform.position + cam.transform.forward);
            }
        }
    }
}
