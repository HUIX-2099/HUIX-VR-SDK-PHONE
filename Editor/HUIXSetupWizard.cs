/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Setup Wizard - Easy VR scene setup
 */

using UnityEngine;
using UnityEditor;
using HUIX.PhoneVR.Core;

namespace HUIX.PhoneVR.Editor
{
    public class HUIXSetupWizard : EditorWindow
    {
        private int _currentPage = 0;
        private bool _createManager = true;
        private bool _createVRCamera = true;
        private bool _createHeadTracker = true;
        private bool _createInputManager = true;
        private bool _createReticle = true;
        private bool _createSampleUI = false;
        private HeadsetProfile _selectedProfile;
        private Vector2 _scrollPos;

        [MenuItem("HUIX/Phone VR/Setup Wizard", false, 0)]
        public static void ShowWindow()
        {
            HUIXSetupWizard window = GetWindow<HUIXSetupWizard>("HUIX VR Setup");
            window.minSize = new Vector2(450, 500);
            window.maxSize = new Vector2(450, 600);
        }

        [MenuItem("HUIX/Phone VR/Quick Setup", false, 1)]
        public static void QuickSetup()
        {
            CreateVRRig();
            Debug.Log("[HUIX VR] Quick setup complete!");
        }

        [MenuItem("HUIX/Phone VR/Documentation", false, 100)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/huix/phone-vr-sdk");
        }

        private void OnGUI()
        {
            DrawHeader();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            switch (_currentPage)
            {
                case 0:
                    DrawWelcomePage();
                    break;
                case 1:
                    DrawComponentsPage();
                    break;
                case 2:
                    DrawHeadsetPage();
                    break;
                case 3:
                    DrawSetupPage();
                    break;
            }

            EditorGUILayout.EndScrollView();

            DrawNavigationButtons();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 0.6f, 1f) }
            };

            EditorGUILayout.LabelField("🎮 HUIX Phone VR SDK", titleStyle);
            EditorGUILayout.LabelField("Setup Wizard", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space(5);
            
            // Progress bar
            Rect progressRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(4));
            float progress = (_currentPage + 1) / 4f;
            EditorGUI.ProgressBar(progressRect, progress, "");
            
            EditorGUILayout.Space(10);
        }

        private void DrawWelcomePage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Welcome!", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField(
                "This wizard will help you set up HUIX Phone VR SDK in your scene. " +
                "Turn any smartphone into a VR headset with stereoscopic rendering, " +
                "head tracking, and intuitive controls.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Features:", EditorStyles.boldLabel);
            DrawFeatureItem("✓ Stereoscopic 3D rendering");
            DrawFeatureItem("✓ Gyroscope-based head tracking");
            DrawFeatureItem("✓ Lens distortion correction");
            DrawFeatureItem("✓ Gaze-based interaction");
            DrawFeatureItem("✓ Customizable headset profiles");
            DrawFeatureItem("✓ Mobile-optimized performance");
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Quick Start", EditorStyles.boldLabel);
            
            if (GUILayout.Button("⚡ One-Click Setup", GUILayout.Height(35)))
            {
                QuickSetup();
                Close();
            }
            
            EditorGUILayout.LabelField("Or continue with the wizard for more options →", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawComponentsPage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Select Components", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Choose which components to add to your scene:", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            _createManager = DrawToggleOption("VR Manager", "Core system manager (required)", _createManager, false);
            _createVRCamera = DrawToggleOption("VR Camera", "Stereoscopic camera system", _createVRCamera);
            _createHeadTracker = DrawToggleOption("Head Tracker", "Gyroscope-based head tracking", _createHeadTracker);
            _createInputManager = DrawToggleOption("Input Manager", "Gaze and trigger input handling", _createInputManager);
            _createReticle = DrawToggleOption("Reticle", "Visual gaze cursor", _createReticle);
            _createSampleUI = DrawToggleOption("Sample UI", "Example VR UI elements", _createSampleUI);
        }

        private void DrawHeadsetPage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Headset Profile", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Select a headset profile or create a custom one:", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            _selectedProfile = (HeadsetProfile)EditorGUILayout.ObjectField("Custom Profile", _selectedProfile, typeof(HeadsetProfile), false);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Or use a preset:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Default\n(Balanced)", GUILayout.Height(50)))
            {
                _selectedProfile = null;
            }
            
            if (GUILayout.Button("Cardboard\n(Wide FOV)", GUILayout.Height(50)))
            {
                _selectedProfile = HeadsetProfile.CreateCardboardProfile();
            }
            
            if (GUILayout.Button("Premium\n(High Quality)", GUILayout.Height(50)))
            {
                _selectedProfile = HeadsetProfile.CreatePremiumProfile();
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (_selectedProfile != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Selected: {_selectedProfile.ProfileName}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"FOV: {_selectedProfile.FieldOfView}°");
                EditorGUILayout.LabelField($"IPD: {_selectedProfile.IPD}mm");
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawSetupPage()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Ready to Setup!", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Review your configuration and click Setup to continue:", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Components to create:", EditorStyles.boldLabel);
            
            if (_createManager) EditorGUILayout.LabelField("  ✓ VR Manager");
            if (_createVRCamera) EditorGUILayout.LabelField("  ✓ VR Camera");
            if (_createHeadTracker) EditorGUILayout.LabelField("  ✓ Head Tracker");
            if (_createInputManager) EditorGUILayout.LabelField("  ✓ Input Manager");
            if (_createReticle) EditorGUILayout.LabelField("  ✓ Reticle");
            if (_createSampleUI) EditorGUILayout.LabelField("  ✓ Sample UI");
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("🚀 Setup VR Scene", GUILayout.Height(45)))
            {
                PerformSetup();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawNavigationButtons()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _currentPage > 0;
            if (GUILayout.Button("← Back", GUILayout.Width(100)))
            {
                _currentPage--;
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"Page {_currentPage + 1} of 4", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(80));

            GUILayout.FlexibleSpace();

            GUI.enabled = _currentPage < 3;
            if (GUILayout.Button("Next →", GUILayout.Width(100)))
            {
                _currentPage++;
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
        }

        private void DrawFeatureItem(string text)
        {
            EditorGUILayout.LabelField(text);
        }

        private bool DrawToggleOption(string title, string description, bool value, bool canDisable = true)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            EditorGUI.BeginDisabledGroup(!canDisable);
            value = EditorGUILayout.Toggle(value, GUILayout.Width(20));
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();

            return value;
        }

        private void PerformSetup()
        {
            CreateVRRig();

            if (_selectedProfile != null)
            {
                HUIXVRManager manager = FindObjectOfType<HUIXVRManager>();
                if (manager != null)
                {
                    manager.SetHeadsetProfile(_selectedProfile);
                }
            }

            Debug.Log("[HUIX VR] Setup complete!");
            
            EditorUtility.DisplayDialog("Setup Complete", 
                "HUIX Phone VR SDK has been set up in your scene!\n\n" +
                "Press Play to test in the editor.\n" +
                "Build to Android/iOS to test on your phone.", 
                "OK");

            Close();
        }

        private static void CreateVRRig()
        {
            // Create VR Rig object
            GameObject rigObj = new GameObject("HUIX VR Rig");
            HUIXVRRig rig = rigObj.AddComponent<HUIXVRRig>();
            
            // Select the new object
            Selection.activeGameObject = rigObj;
            
            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
        }
    }
}

