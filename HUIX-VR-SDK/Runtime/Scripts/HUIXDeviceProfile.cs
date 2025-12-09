/*
 * HUIX-VR-SDK-PHONE
 * Device Profile - Preset configurations for different VR viewers
 */

using UnityEngine;

namespace HUIX.VR
{
    /// <summary>
    /// Device profile for VR headset configurations
    /// </summary>
    [CreateAssetMenu(fileName = "HUIXDeviceProfile", menuName = "HUIX VR/Device Profile")]
    public class HUIXDeviceProfile : ScriptableObject
    {
        [Header("Device Info")]
        public string vendor = "Generic";
        public string model = "Cardboard";
        
        [Header("Lens Configuration")]
        [Tooltip("Inter-pupillary distance in meters")]
        [Range(0.055f, 0.075f)]
        public float ipd = 0.064f;
        
        [Tooltip("Screen to lens distance in meters")]
        [Range(0.025f, 0.06f)]
        public float screenToLensDistance = 0.042f;
        
        [Header("Distortion")]
        [Tooltip("Radial distortion coefficient K1")]
        public float distortionK1 = 0.441f;
        
        [Tooltip("Radial distortion coefficient K2")]
        public float distortionK2 = 0.156f;
        
        [Header("Field of View")]
        [Tooltip("Field of view in degrees")]
        [Range(60f, 120f)]
        public float fieldOfView = 95f;
        
        /// <summary>
        /// Apply this profile to a HUIX VR Camera
        /// </summary>
        public void ApplyTo(HUIXVRCamera camera)
        {
            if (camera == null) return;
            
            camera.ipd = ipd;
            camera.screenToLens = screenToLensDistance;
            camera.distortionK1 = distortionK1;
            camera.distortionK2 = distortionK2;
            camera.fieldOfView = fieldOfView;
        }
        
        #region Static Factory Methods
        
        /// <summary>
        /// Get Google Cardboard V1 profile values
        /// </summary>
        public static HUIXDeviceProfile CreateCardboardV1()
        {
            var profile = CreateInstance<HUIXDeviceProfile>();
            profile.vendor = "Google, Inc.";
            profile.model = "Cardboard v1";
            profile.ipd = 0.06f;
            profile.screenToLensDistance = 0.042f;
            profile.distortionK1 = 0.441f;
            profile.distortionK2 = 0.156f;
            profile.fieldOfView = 90f;
            return profile;
        }
        
        /// <summary>
        /// Get Google Cardboard V2 profile values
        /// </summary>
        public static HUIXDeviceProfile CreateCardboardV2()
        {
            var profile = CreateInstance<HUIXDeviceProfile>();
            profile.vendor = "Google, Inc.";
            profile.model = "Cardboard v2";
            profile.ipd = 0.064f;
            profile.screenToLensDistance = 0.039f;
            profile.distortionK1 = 0.34f;
            profile.distortionK2 = 0.55f;
            profile.fieldOfView = 95f;
            return profile;
        }
        
        /// <summary>
        /// Get generic/default profile values
        /// </summary>
        public static HUIXDeviceProfile CreateGeneric()
        {
            var profile = CreateInstance<HUIXDeviceProfile>();
            profile.vendor = "Generic";
            profile.model = "VR Viewer";
            profile.ipd = 0.064f;
            profile.screenToLensDistance = 0.04f;
            profile.distortionK1 = 0.4f;
            profile.distortionK2 = 0.2f;
            profile.fieldOfView = 100f;
            return profile;
        }
        
        #endregion
    }
}

