/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Headset Profile Custom Editor
 */

using UnityEngine;
using UnityEditor;
using HUIX.PhoneVR.Core;

namespace HUIX.PhoneVR.Editor
{
    [CustomEditor(typeof(HeadsetProfile))]
    public class HUIXHeadsetProfileEditor : UnityEditor.Editor
    {
        private bool _showLensPreview = true;
        private Texture2D _previewTexture;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            HeadsetProfile profile = (HeadsetProfile)target;

            // Header
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("👓 " + profile.ProfileName, headerStyle);
            EditorGUILayout.LabelField(profile.Manufacturer, EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Draw default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Lens preview
            _showLensPreview = EditorGUILayout.Foldout(_showLensPreview, "Lens Distortion Preview", true);
            if (_showLensPreview)
            {
                DrawLensPreview(profile);
            }

            EditorGUILayout.Space(10);

            // Validation
            DrawValidation(profile);

            // Preset buttons
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Load Preset Values", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Cardboard"))
            {
                LoadPreset(profile, HeadsetProfile.CreateCardboardProfile());
            }
            
            if (GUILayout.Button("Premium"))
            {
                LoadPreset(profile, HeadsetProfile.CreatePremiumProfile());
            }
            
            if (GUILayout.Button("Reset Default"))
            {
                LoadPreset(profile, HeadsetProfile.CreateDefault());
            }
            
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLensPreview(HeadsetProfile profile)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Create preview texture if needed
            if (_previewTexture == null)
            {
                _previewTexture = new Texture2D(200, 200);
            }

            // Generate distortion preview
            GenerateDistortionPreview(profile);

            // Draw preview
            Rect previewRect = GUILayoutUtility.GetRect(200, 150);
            previewRect.x = (EditorGUIUtility.currentViewWidth - 200) / 2;
            previewRect.width = 200;
            
            GUI.DrawTexture(previewRect, _previewTexture, ScaleMode.ScaleToFit);

            EditorGUILayout.LabelField("Grid shows barrel distortion effect", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.EndVertical();
        }

        private void GenerateDistortionPreview(HeadsetProfile profile)
        {
            int size = 200;
            float k1 = profile.DistortionK1;
            float k2 = profile.DistortionK2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size - 0.5f;
                    float v = (float)y / size - 0.5f;
                    
                    float r2 = u * u + v * v;
                    float r4 = r2 * r2;
                    
                    float distortion = 1f + k1 * r2 + k2 * r4;
                    
                    float distU = u * distortion + 0.5f;
                    float distV = v * distortion + 0.5f;

                    Color color = Color.black;

                    // Draw grid
                    int gridX = Mathf.RoundToInt(distU * 10);
                    int gridY = Mathf.RoundToInt(distV * 10);

                    bool onGrid = (Mathf.Abs(distU * 10 - gridX) < 0.1f) || (Mathf.Abs(distV * 10 - gridY) < 0.1f);

                    if (distU >= 0 && distU <= 1 && distV >= 0 && distV <= 1)
                    {
                        if (onGrid)
                        {
                            color = new Color(0.2f, 0.6f, 1f);
                        }
                        else
                        {
                            color = new Color(0.1f, 0.1f, 0.15f);
                        }
                    }

                    _previewTexture.SetPixel(x, y, color);
                }
            }

            _previewTexture.Apply();
        }

        private void DrawValidation(HeadsetProfile profile)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            bool isValid = true;
            string message = "Profile is valid ✓";

            if (profile.FieldOfView < 60 || profile.FieldOfView > 120)
            {
                isValid = false;
                message = "⚠ FOV should be between 60° and 120°";
            }
            else if (profile.IPD < 50 || profile.IPD > 80)
            {
                isValid = false;
                message = "⚠ IPD should be between 50mm and 80mm";
            }
            else if (profile.ScreenToLensDistance <= 0)
            {
                isValid = false;
                message = "✗ Screen to lens distance must be positive";
            }

            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = isValid ? Color.green : Color.yellow }
            };

            EditorGUILayout.LabelField(message, style);
            EditorGUILayout.EndVertical();
        }

        private void LoadPreset(HeadsetProfile target, HeadsetProfile preset)
        {
            Undo.RecordObject(target, "Load Preset");

            target.FieldOfView = preset.FieldOfView;
            target.DistortionK1 = preset.DistortionK1;
            target.DistortionK2 = preset.DistortionK2;
            target.ScreenToLensDistance = preset.ScreenToLensDistance;
            target.EnableChromaticCorrection = preset.EnableChromaticCorrection;
            target.ChromaticRed = preset.ChromaticRed;
            target.ChromaticGreen = preset.ChromaticGreen;
            target.ChromaticBlue = preset.ChromaticBlue;

            EditorUtility.SetDirty(target);
        }

        private void OnDisable()
        {
            if (_previewTexture != null)
            {
                DestroyImmediate(_previewTexture);
            }
        }
    }
}

