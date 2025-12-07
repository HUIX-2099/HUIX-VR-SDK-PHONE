/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Menu Items - Editor menu shortcuts
 */

using UnityEngine;
using UnityEditor;
using HUIX.PhoneVR;
using HUIX.PhoneVR.Core;
using HUIX.PhoneVR.UI;

namespace HUIX.PhoneVR.Editor
{
    public static class HUIXMenuItems
    {
        private const string MENU_ROOT = "HUIX/Phone VR/";
        private const string GAMEOBJECT_MENU = "GameObject/HUIX Phone VR/";

        #region Main Menu Items
        
        [MenuItem(MENU_ROOT + "Setup Wizard", false, 0)]
        public static void OpenSetupWizard()
        {
            HUIXSetupWizard.ShowWindow();
        }

        [MenuItem(MENU_ROOT + "Quick Setup", false, 1)]
        public static void QuickSetup()
        {
            HUIXSetupWizard.QuickSetup();
        }

        [MenuItem(MENU_ROOT + "Documentation", false, 100)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/huix/phone-vr-sdk");
        }

        [MenuItem(MENU_ROOT + "About", false, 101)]
        public static void ShowAbout()
        {
            EditorUtility.DisplayDialog(
                "HUIX Phone VR SDK",
                "Version 1.0.0\n\n" +
                "Transform any smartphone into a VR headset.\n\n" +
                "© 2024 HUIX\n" +
                "https://huix.dev",
                "OK"
            );
        }

        #endregion

        #region GameObject Menu Items

        [MenuItem(GAMEOBJECT_MENU + "VR Rig", false, 10)]
        public static void CreateVRRig()
        {
            GameObject rig = new GameObject("HUIX VR Rig");
            rig.AddComponent<HUIXVRRig>();
            Selection.activeGameObject = rig;
            Undo.RegisterCreatedObjectUndo(rig, "Create VR Rig");
        }

        [MenuItem(GAMEOBJECT_MENU + "VR Camera", false, 11)]
        public static void CreateVRCamera()
        {
            GameObject camera = new GameObject("VR Camera");
            camera.AddComponent<Camera>();
            camera.AddComponent<HUIXVRCamera>();
            camera.AddComponent<AudioListener>();
            camera.tag = "MainCamera";
            Selection.activeGameObject = camera;
            Undo.RegisterCreatedObjectUndo(camera, "Create VR Camera");
        }

        [MenuItem(GAMEOBJECT_MENU + "Head Tracker", false, 12)]
        public static void CreateHeadTracker()
        {
            GameObject tracker = new GameObject("Head Tracker");
            tracker.AddComponent<HUIXHeadTracker>();
            Selection.activeGameObject = tracker;
            Undo.RegisterCreatedObjectUndo(tracker, "Create Head Tracker");
        }

        [MenuItem(GAMEOBJECT_MENU + "Input Manager", false, 13)]
        public static void CreateInputManager()
        {
            GameObject input = new GameObject("Input Manager");
            input.AddComponent<HUIXInputManager>();
            Selection.activeGameObject = input;
            Undo.RegisterCreatedObjectUndo(input, "Create Input Manager");
        }

        [MenuItem(GAMEOBJECT_MENU + "Reticle", false, 14)]
        public static void CreateReticle()
        {
            GameObject reticle = new GameObject("Reticle");
            reticle.AddComponent<HUIXReticle>();
            Selection.activeGameObject = reticle;
            Undo.RegisterCreatedObjectUndo(reticle, "Create Reticle");
        }

        [MenuItem(GAMEOBJECT_MENU + "UI/VR Button", false, 20)]
        public static void CreateVRButton()
        {
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = "VR Button";
            button.transform.localScale = new Vector3(0.3f, 0.1f, 0.1f);
            button.AddComponent<HUIXVRButton>();
            Selection.activeGameObject = button;
            Undo.RegisterCreatedObjectUndo(button, "Create VR Button");
        }

        [MenuItem(GAMEOBJECT_MENU + "UI/VR Slider", false, 21)]
        public static void CreateVRSlider()
        {
            GameObject slider = new GameObject("VR Slider");
            slider.AddComponent<HUIXVRSlider>();
            
            // Create background
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "Background";
            bg.transform.SetParent(slider.transform);
            bg.transform.localScale = new Vector3(1f, 0.1f, 0.05f);
            bg.transform.localPosition = Vector3.zero;
            
            // Create fill
            GameObject fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Fill";
            fill.transform.SetParent(slider.transform);
            fill.transform.localScale = new Vector3(0.5f, 0.1f, 0.06f);
            fill.transform.localPosition = new Vector3(-0.25f, 0, -0.01f);
            fill.GetComponent<Renderer>().material.color = new Color(0.2f, 0.6f, 1f);
            
            // Create handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = "Handle";
            handle.transform.SetParent(slider.transform);
            handle.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            handle.transform.localPosition = Vector3.zero;
            
            Selection.activeGameObject = slider;
            Undo.RegisterCreatedObjectUndo(slider, "Create VR Slider");
        }

        [MenuItem(GAMEOBJECT_MENU + "UI/VR Canvas", false, 22)]
        public static void CreateVRCanvas()
        {
            GameObject canvas = new GameObject("VR Canvas");
            Canvas c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            canvas.AddComponent<HUIXVRCanvas>();
            
            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 150);
            rt.localScale = Vector3.one * 0.01f;
            rt.position = new Vector3(0, 1.5f, 3f);
            
            Selection.activeGameObject = canvas;
            Undo.RegisterCreatedObjectUndo(canvas, "Create VR Canvas");
        }

        [MenuItem(GAMEOBJECT_MENU + "Teleporter", false, 30)]
        public static void CreateTeleporter()
        {
            GameObject teleporter = new GameObject("Teleporter");
            teleporter.AddComponent<HUIXTeleporter>();
            Selection.activeGameObject = teleporter;
            Undo.RegisterCreatedObjectUndo(teleporter, "Create Teleporter");
        }

        #endregion

        #region Asset Menu Items

        [MenuItem("Assets/Create/HUIX/Phone VR/Headset Profile", false, 0)]
        public static void CreateHeadsetProfile()
        {
            HeadsetProfile profile = ScriptableObject.CreateInstance<HeadsetProfile>();
            
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(path))
            {
                path = "Assets";
            }
            else if (System.IO.Path.GetExtension(path) != "")
            {
                path = path.Replace(System.IO.Path.GetFileName(path), "");
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath(path + "/NewHeadsetProfile.asset");
            AssetDatabase.CreateAsset(profile, assetPath);
            AssetDatabase.SaveAssets();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = profile;
        }

        #endregion

        #region Validation

        [MenuItem(MENU_ROOT + "Validate Scene", false, 50)]
        public static void ValidateScene()
        {
            bool hasManager = Object.FindObjectOfType<HUIXVRManager>() != null;
            bool hasCamera = Object.FindObjectOfType<HUIXVRCamera>() != null;
            bool hasTracker = Object.FindObjectOfType<HUIXHeadTracker>() != null;
            bool hasInput = Object.FindObjectOfType<HUIXInputManager>() != null;

            string message = "Scene Validation Results:\n\n";
            message += hasManager ? "✓ VR Manager found\n" : "✗ VR Manager missing\n";
            message += hasCamera ? "✓ VR Camera found\n" : "✗ VR Camera missing\n";
            message += hasTracker ? "✓ Head Tracker found\n" : "✗ Head Tracker missing\n";
            message += hasInput ? "✓ Input Manager found\n" : "✗ Input Manager missing\n";

            if (hasManager && hasCamera && hasTracker && hasInput)
            {
                message += "\n✓ Scene is properly configured for VR!";
            }
            else
            {
                message += "\n⚠ Some components are missing. Use Quick Setup to add them.";
            }

            EditorUtility.DisplayDialog("HUIX VR - Scene Validation", message, "OK");
        }

        #endregion
    }
}

