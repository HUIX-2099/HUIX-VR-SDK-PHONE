/*
 * HUIX-VR-SDK-PHONE
 * Editor Setup Helper - Quick menu for adding VR to your scene
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace HUIX.VR.Editor
{
    public static class HUIXVRSetup
    {
        private const string MENU_PATH = "HUIX VR/";
        
        [MenuItem(MENU_PATH + "Add VR to Main Camera", false, 0)]
        public static void AddVRToMainCamera()
        {
            Camera mainCamera = Camera.main;
            
            if (mainCamera == null)
            {
                EditorUtility.DisplayDialog("HUIX VR Setup", 
                    "No Main Camera found in scene!\n\nCreate a camera with the 'MainCamera' tag first.", 
                    "OK");
                return;
            }
            
            if (mainCamera.GetComponent<HUIXVRCamera>() != null)
            {
                EditorUtility.DisplayDialog("HUIX VR Setup", 
                    "Main Camera already has HUIX VR Camera attached!", 
                    "OK");
                Selection.activeGameObject = mainCamera.gameObject;
                return;
            }
            
            Undo.AddComponent<HUIXVRCamera>(mainCamera.gameObject);
            Selection.activeGameObject = mainCamera.gameObject;
            
            EditorUtility.DisplayDialog("HUIX VR Setup", 
                "✓ HUIX VR Camera added to Main Camera!\n\nYou're ready to go - just press Play!", 
                "Awesome!");
        }
        
        [MenuItem(MENU_PATH + "Create VR Camera", false, 1)]
        public static void CreateVRCamera()
        {
            GameObject cameraObj = new GameObject("HUIX VR Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cameraObj.AddComponent<HUIXVRCamera>();
            
            // Add audio listener if none exists
            if (Object.FindObjectOfType<AudioListener>() == null)
            {
                cameraObj.AddComponent<AudioListener>();
            }
            
            Undo.RegisterCreatedObjectUndo(cameraObj, "Create HUIX VR Camera");
            Selection.activeGameObject = cameraObj;
            
            EditorUtility.DisplayDialog("HUIX VR Setup", 
                "✓ HUIX VR Camera created!\n\nYou're ready to go - just press Play!", 
                "Awesome!");
        }
        
        [MenuItem(MENU_PATH + "Apply Cardboard V1 Profile", false, 20)]
        public static void ApplyCardboardV1()
        {
            ApplyProfileToSelected(profile => {
                profile.ipd = 0.06f;
                profile.screenToLens = 0.042f;
                profile.distortionK1 = 0.441f;
                profile.distortionK2 = 0.156f;
                profile.fieldOfView = 90f;
            }, "Cardboard V1");
        }
        
        [MenuItem(MENU_PATH + "Apply Cardboard V2 Profile", false, 21)]
        public static void ApplyCardboardV2()
        {
            ApplyProfileToSelected(profile => {
                profile.ipd = 0.064f;
                profile.screenToLens = 0.039f;
                profile.distortionK1 = 0.34f;
                profile.distortionK2 = 0.55f;
                profile.fieldOfView = 95f;
            }, "Cardboard V2");
        }
        
        [MenuItem(MENU_PATH + "Apply Generic VR Profile", false, 22)]
        public static void ApplyGenericProfile()
        {
            ApplyProfileToSelected(profile => {
                profile.ipd = 0.064f;
                profile.screenToLens = 0.04f;
                profile.distortionK1 = 0.4f;
                profile.distortionK2 = 0.2f;
                profile.fieldOfView = 100f;
            }, "Generic VR");
        }
        
        private static void ApplyProfileToSelected(System.Action<HUIXVRCamera> apply, string profileName)
        {
            HUIXVRCamera vrCamera = null;
            
            // Try to get from selection
            if (Selection.activeGameObject != null)
            {
                vrCamera = Selection.activeGameObject.GetComponent<HUIXVRCamera>();
            }
            
            // Try to find any in scene
            if (vrCamera == null)
            {
                vrCamera = Object.FindObjectOfType<HUIXVRCamera>();
            }
            
            if (vrCamera == null)
            {
                EditorUtility.DisplayDialog("HUIX VR Setup", 
                    "No HUIX VR Camera found!\n\nUse 'HUIX VR > Add VR to Main Camera' first.", 
                    "OK");
                return;
            }
            
            Undo.RecordObject(vrCamera, $"Apply {profileName} Profile");
            apply(vrCamera);
            EditorUtility.SetDirty(vrCamera);
            
            Debug.Log($"[HUIX VR] Applied {profileName} profile to {vrCamera.gameObject.name}");
        }
        
        [MenuItem(MENU_PATH + "Documentation", false, 100)]
        public static void OpenDocumentation()
        {
            EditorUtility.DisplayDialog("HUIX VR SDK - Quick Start", 
@"=== HUIX VR SDK - QUICK START ===

1. Add VR to your camera:
   • Select Main Camera
   • Add Component > HUIX VR Camera
   OR use menu: HUIX VR > Add VR to Main Camera

2. Press Play!

That's it! No demos, no complex setup.

=== OPTIONAL SETTINGS ===

• IPD: Eye distance (default: 0.064m)
• Distortion K1/K2: Lens correction
• FOV: Field of view (default: 95°)
• Enable Distortion: Barrel distortion on/off
• Show Divider: Center line between eyes

=== RUNTIME API ===

var vr = GetComponent<HUIXVRCamera>();
vr.Recenter();      // Reset view direction
vr.ToggleVR();      // Enable/disable VR mode

=== PROFILES ===

Use menu 'HUIX VR > Apply Cardboard V1/V2 Profile'
to quickly match Google Cardboard settings.", 
                "Got it!");
        }
        
        // Validation
        [MenuItem(MENU_PATH + "Add VR to Main Camera", true)]
        [MenuItem(MENU_PATH + "Apply Cardboard V1 Profile", true)]
        [MenuItem(MENU_PATH + "Apply Cardboard V2 Profile", true)]
        [MenuItem(MENU_PATH + "Apply Generic VR Profile", true)]
        private static bool ValidateSceneLoaded()
        {
            return !string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
    
    /// <summary>
    /// Custom inspector for HUIX VR Camera
    /// </summary>
    [CustomEditor(typeof(HUIXVRCamera))]
    public class HUIXVRCameraEditor : UnityEditor.Editor
    {
        private bool showAdvanced = false;
        
        public override void OnInspectorGUI()
        {
            HUIXVRCamera vrCamera = (HUIXVRCamera)target;
            
            // Header
            EditorGUILayout.Space();
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("HUIX VR SDK", headerStyle);
            EditorGUILayout.LabelField("Phone VR Made Simple", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space();
            
            // Quick Actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cardboard V1", EditorStyles.miniButtonLeft))
            {
                Undo.RecordObject(vrCamera, "Apply Cardboard V1");
                vrCamera.ApplyCardboardV1Profile();
            }
            if (GUILayout.Button("Cardboard V2", EditorStyles.miniButtonRight))
            {
                Undo.RecordObject(vrCamera, "Apply Cardboard V2");
                vrCamera.ApplyCardboardV2Profile();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            // Runtime buttons (only in play mode)
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Recenter View"))
                {
                    vrCamera.Recenter();
                }
                if (GUILayout.Button("Toggle VR"))
                {
                    vrCamera.ToggleVR();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif

