/*
 * HUIX Phone VR SDK
 * Copyright (c) 2024 HUIX
 * 
 * Headset Profile - Configuration for different VR headset types
 */

using UnityEngine;

namespace HUIX.PhoneVR.Core
{
    /// <summary>
    /// Defines the optical and physical properties of a phone VR headset.
    /// Create custom profiles to support different headset models.
    /// </summary>
    [CreateAssetMenu(fileName = "New Headset Profile", menuName = "HUIX/Phone VR/Headset Profile")]
    public class HeadsetProfile : ScriptableObject
    {
        [Header("=== Headset Information ===")]
        [Tooltip("Name of the headset profile")]
        public string ProfileName = "Default Headset";
        
        [Tooltip("Manufacturer or brand")]
        public string Manufacturer = "Generic";
        
        [TextArea(2, 4)]
        [Tooltip("Description of this headset")]
        public string Description = "A generic phone VR headset profile";

        [Header("=== Display Settings ===")]
        [Tooltip("Screen width in meters")]
        [Range(0.05f, 0.2f)]
        public float ScreenWidth = 0.11f;
        
        [Tooltip("Screen height in meters")]
        [Range(0.03f, 0.15f)]
        public float ScreenHeight = 0.062f;
        
        [Tooltip("Distance from screen to lenses in meters")]
        [Range(0.02f, 0.1f)]
        public float ScreenToLensDistance = 0.042f;

        [Header("=== Lens Configuration ===")]
        [Tooltip("Inter-lens distance in millimeters")]
        [Range(50f, 80f)]
        public float InterLensDistance = 64f;
        
        [Tooltip("Inter-Pupillary Distance in millimeters (user setting)")]
        [Range(50f, 80f)]
        public float IPD = 64f;
        
        [Tooltip("Vertical offset of lenses from center in meters")]
        [Range(-0.02f, 0.02f)]
        public float LensVerticalOffset = 0f;

        [Header("=== Field of View ===")]
        [Tooltip("Horizontal field of view in degrees")]
        [Range(60f, 120f)]
        public float FieldOfView = 100f;
        
        [Tooltip("Vertical field of view multiplier")]
        [Range(0.5f, 1.5f)]
        public float VerticalFOVMultiplier = 1f;

        [Header("=== Distortion Correction ===")]
        [Tooltip("Enable lens distortion correction")]
        public bool EnableDistortionCorrection = true;
        
        [Tooltip("Barrel distortion coefficient K1")]
        [Range(0f, 1f)]
        public float DistortionK1 = 0.34f;
        
        [Tooltip("Barrel distortion coefficient K2")]
        [Range(0f, 1f)]
        public float DistortionK2 = 0.55f;

        [Header("=== Chromatic Aberration ===")]
        [Tooltip("Enable chromatic aberration correction")]
        public bool EnableChromaticCorrection = true;
        
        [Tooltip("Red channel offset")]
        [Range(-0.1f, 0.1f)]
        public float ChromaticRed = -0.006f;
        
        [Tooltip("Green channel offset")]
        [Range(-0.1f, 0.1f)]
        public float ChromaticGreen = 0f;
        
        [Tooltip("Blue channel offset")]
        [Range(-0.1f, 0.1f)]
        public float ChromaticBlue = 0.014f;

        [Header("=== Color Correction ===")]
        [Tooltip("Brightness adjustment")]
        [Range(0.5f, 2f)]
        public float Brightness = 1f;
        
        [Tooltip("Contrast adjustment")]
        [Range(0.5f, 2f)]
        public float Contrast = 1f;
        
        [Tooltip("Saturation adjustment")]
        [Range(0f, 2f)]
        public float Saturation = 1f;

        /// <summary>
        /// Creates a default headset profile at runtime
        /// </summary>
        public static HeadsetProfile CreateDefault()
        {
            HeadsetProfile profile = CreateInstance<HeadsetProfile>();
            profile.ProfileName = "HUIX Default";
            profile.Description = "Default profile optimized for most phone VR headsets";
            return profile;
        }

        /// <summary>
        /// Creates a Google Cardboard-like profile
        /// </summary>
        public static HeadsetProfile CreateCardboardProfile()
        {
            HeadsetProfile profile = CreateInstance<HeadsetProfile>();
            profile.ProfileName = "Cardboard Style";
            profile.Manufacturer = "Generic Cardboard";
            profile.Description = "Profile for Google Cardboard-style headsets";
            profile.FieldOfView = 90f;
            profile.DistortionK1 = 0.441f;
            profile.DistortionK2 = 0.156f;
            profile.ScreenToLensDistance = 0.039f;
            return profile;
        }

        /// <summary>
        /// Creates a high-quality headset profile
        /// </summary>
        public static HeadsetProfile CreatePremiumProfile()
        {
            HeadsetProfile profile = CreateInstance<HeadsetProfile>();
            profile.ProfileName = "Premium VR";
            profile.Manufacturer = "High-End";
            profile.Description = "Profile for premium phone VR headsets with better optics";
            profile.FieldOfView = 110f;
            profile.DistortionK1 = 0.22f;
            profile.DistortionK2 = 0.24f;
            profile.ScreenToLensDistance = 0.045f;
            profile.EnableChromaticCorrection = true;
            return profile;
        }

        /// <summary>
        /// Get the eye separation in Unity units (meters)
        /// </summary>
        public float GetEyeSeparation()
        {
            return IPD / 1000f; // Convert mm to meters
        }

        /// <summary>
        /// Get aspect ratio of the display
        /// </summary>
        public float GetAspectRatio()
        {
            return ScreenWidth / ScreenHeight;
        }

        /// <summary>
        /// Validate profile values
        /// </summary>
        public bool Validate()
        {
            bool valid = true;

            if (ScreenWidth <= 0 || ScreenHeight <= 0)
            {
                Debug.LogError($"[HUIX VR] Invalid screen dimensions in profile: {ProfileName}");
                valid = false;
            }

            if (FieldOfView <= 0 || FieldOfView > 180)
            {
                Debug.LogError($"[HUIX VR] Invalid FOV in profile: {ProfileName}");
                valid = false;
            }

            if (IPD < 50 || IPD > 80)
            {
                Debug.LogWarning($"[HUIX VR] Unusual IPD value in profile: {ProfileName}. Normal range is 50-80mm.");
            }

            return valid;
        }

        private void OnValidate()
        {
            // Ensure lens distance doesn't exceed IPD
            InterLensDistance = Mathf.Min(InterLensDistance, IPD + 5f);
        }
    }
}

