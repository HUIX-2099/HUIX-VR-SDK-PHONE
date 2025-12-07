/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * VR Manager Custom Editor
 */

using UnityEngine;
using UnityEditor;
using HUIX.PhoneVR.Core;

namespace HUIX.PhoneVR.Editor
{
    [CustomEditor(typeof(HUIXVRManager))]
    public class HUIXVRManagerEditor : UnityEditor.Editor
    {
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private bool _showDebugSection = true;
        private bool _showPerformanceSection = true;

        private SerializedProperty _autoInitialize;
        private SerializedProperty _vrModeOnStart;
        private SerializedProperty _persistAcrossScenes;
        private SerializedProperty _headsetProfile;
        private SerializedProperty _targetFrameRate;
        private SerializedProperty _adaptiveQuality;
        private SerializedProperty _renderScale;
        private SerializedProperty _showDebugInfo;
        private SerializedProperty _simulateInEditor;

        private void OnEnable()
        {
            _autoInitialize = serializedObject.FindProperty("_autoInitialize");
            _vrModeOnStart = serializedObject.FindProperty("_vrModeOnStart");
            _persistAcrossScenes = serializedObject.FindProperty("_persistAcrossScenes");
            _headsetProfile = serializedObject.FindProperty("_headsetProfile");
            _targetFrameRate = serializedObject.FindProperty("_targetFrameRate");
            _adaptiveQuality = serializedObject.FindProperty("_adaptiveQuality");
            _renderScale = serializedObject.FindProperty("_renderScale");
            _showDebugInfo = serializedObject.FindProperty("_showDebugInfo");
            _simulateInEditor = serializedObject.FindProperty("_simulateInEditor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            InitStyles();

            // Header
            DrawHeader();

            EditorGUILayout.Space(10);

            // General Settings
            DrawSection("General Settings", () =>
            {
                EditorGUILayout.PropertyField(_autoInitialize, new GUIContent("Auto Initialize", "Initialize VR system on Awake"));
                EditorGUILayout.PropertyField(_vrModeOnStart, new GUIContent("VR Mode On Start", "Enable VR mode when scene starts"));
                EditorGUILayout.PropertyField(_persistAcrossScenes, new GUIContent("Persist Across Scenes", "Don't destroy when loading new scenes"));
            });

            EditorGUILayout.Space(5);

            // Headset Configuration
            DrawSection("Headset Configuration", () =>
            {
                EditorGUILayout.PropertyField(_headsetProfile, new GUIContent("Headset Profile", "Current headset configuration"));
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Create New Profile"))
                {
                    CreateHeadsetProfile();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Load Cardboard"))
                {
                    LoadPresetProfile("Cardboard");
                }
                if (GUILayout.Button("Load Premium"))
                {
                    LoadPresetProfile("Premium");
                }
                EditorGUILayout.EndHorizontal();
            });

            EditorGUILayout.Space(5);

            // Performance
            _showPerformanceSection = DrawFoldoutSection("Performance", _showPerformanceSection, () =>
            {
                EditorGUILayout.PropertyField(_targetFrameRate, new GUIContent("Target Frame Rate"));
                EditorGUILayout.PropertyField(_adaptiveQuality, new GUIContent("Adaptive Quality", "Automatically adjust render scale based on performance"));
                
                EditorGUI.BeginDisabledGroup(_adaptiveQuality.boolValue);
                EditorGUILayout.PropertyField(_renderScale, new GUIContent("Render Scale"));
                EditorGUI.EndDisabledGroup();
            });

            EditorGUILayout.Space(5);

            // Debug
            _showDebugSection = DrawFoldoutSection("Debug", _showDebugSection, () =>
            {
                EditorGUILayout.PropertyField(_showDebugInfo, new GUIContent("Show Debug Info", "Display FPS and other info in VR"));
                EditorGUILayout.PropertyField(_simulateInEditor, new GUIContent("Simulate In Editor", "Enable mouse-based head tracking in editor"));
            });

            EditorGUILayout.Space(10);

            // Runtime Controls
            if (Application.isPlaying)
            {
                DrawRuntimeControls();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.2f, 0.6f, 1f) }
                };
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUILayout.LabelField("🎮 HUIX Phone VR SDK", _headerStyle);
            EditorGUILayout.LabelField("Version 1.0.0", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawSection(string title, System.Action content)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
            content?.Invoke();
            EditorGUILayout.EndVertical();
        }

        private bool DrawFoldoutSection(string title, bool foldout, System.Action content)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
            
            if (foldout)
            {
                EditorGUILayout.Space(2);
                content?.Invoke();
            }
            
            EditorGUILayout.EndVertical();
            return foldout;
        }

        private void DrawRuntimeControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
            
            HUIXVRManager manager = (HUIXVRManager)target;

            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = manager.IsVRModeActive ? Color.green : Color.white;
            if (GUILayout.Button(manager.IsVRModeActive ? "Disable VR" : "Enable VR"))
            {
                manager.ToggleVRMode();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Recenter"))
            {
                manager.Recenter();
            }
            
            EditorGUILayout.EndHorizontal();

            // Status
            EditorGUILayout.LabelField("Status:", manager.IsVRModeActive ? "VR Mode Active" : "VR Mode Disabled");
            EditorGUILayout.LabelField("FPS:", manager.GetCurrentFPS().ToString("F1"));
            
            EditorGUILayout.EndVertical();
        }

        private void CreateHeadsetProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create Headset Profile", "NewHeadsetProfile", "asset", "Choose location for headset profile");
            
            if (!string.IsNullOrEmpty(path))
            {
                HeadsetProfile profile = ScriptableObject.CreateInstance<HeadsetProfile>();
                AssetDatabase.CreateAsset(profile, path);
                AssetDatabase.SaveAssets();
                
                _headsetProfile.objectReferenceValue = profile;
                serializedObject.ApplyModifiedProperties();
                
                Selection.activeObject = profile;
            }
        }

        private void LoadPresetProfile(string preset)
        {
            HeadsetProfile profile = null;
            
            switch (preset)
            {
                case "Cardboard":
                    profile = HeadsetProfile.CreateCardboardProfile();
                    break;
                case "Premium":
                    profile = HeadsetProfile.CreatePremiumProfile();
                    break;
            }

            if (profile != null)
            {
                string path = $"Assets/HUIX/{preset}Profile.asset";
                AssetDatabase.CreateAsset(profile, path);
                AssetDatabase.SaveAssets();
                
                _headsetProfile.objectReferenceValue = profile;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}

