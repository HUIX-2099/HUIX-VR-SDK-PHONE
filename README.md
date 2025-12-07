# 🎮 HUIX Phone VR SDK

<p align="center">
  <img src="Documentation~/Images/banner.png" alt="HUIX Phone VR SDK" width="600">
</p>

<p align="center">
  <strong>Transform any smartphone into a VR headset with Unity</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#installation">Installation</a> •
  <a href="#quick-start">Quick Start</a> •
  <a href="#documentation">Documentation</a> •
  <a href="#support">Support</a>
</p>

---

## ✨ Features

**HUIX Phone VR SDK** is a complete solution for creating phone-based VR experiences in Unity. Turn any smartphone into a VR headset with our comprehensive toolkit.

### Core Features

- 🔲 **Stereoscopic 3D Rendering** - Split-screen rendering optimized for VR headsets
- 📱 **Head Tracking** - Gyroscope and accelerometer-based head rotation tracking
- 🔍 **Lens Distortion Correction** - Barrel distortion and chromatic aberration correction
- 👁️ **Gaze-Based Input** - Look at objects to interact with them
- 🎯 **Customizable Reticle** - Multiple reticle styles with dwell-select support
- ⚙️ **Headset Profiles** - Pre-built and customizable profiles for different headsets
- 🚀 **Mobile Optimized** - Built for performance on mobile devices

### Additional Features

- 📐 **VR UI System** - World-space UI optimized for VR (buttons, sliders, canvases)
- 🏃 **Teleportation** - Gaze-based locomotion system
- 🎨 **Editor Tools** - Setup wizard and custom inspectors
- 📊 **Adaptive Quality** - Dynamic render scale based on performance
- 🔧 **Highly Configurable** - Extensive settings for fine-tuning

---

## 📦 Installation

### Option 1: Unity Package Manager (Recommended)

1. Open Unity and go to **Window > Package Manager**
2. Click the **+** button and select **Add package from git URL**
3. Enter: `https://github.com/huix/phone-vr-sdk.git`

### Option 2: Manual Installation

1. Download or clone this repository
2. Copy the `HUIX-SDK` folder into your project's `Packages` folder
3. Unity will automatically import the package

### Option 3: Import from Disk

1. Download the release `.unitypackage` file
2. In Unity, go to **Assets > Import Package > Custom Package**
3. Select the downloaded file and import

---

## 🚀 Quick Start

### One-Click Setup

1. Go to **HUIX > Phone VR > Quick Setup** in the Unity menu
2. Done! A complete VR rig is added to your scene

### Using the Setup Wizard

1. Go to **HUIX > Phone VR > Setup Wizard**
2. Follow the step-by-step wizard to configure your VR scene
3. Choose components and headset profile
4. Click **Setup VR Scene**

### Manual Setup

```csharp
using UnityEngine;
using HUIX.PhoneVR;
using HUIX.PhoneVR.Core;

public class VRSetup : MonoBehaviour
{
    void Start()
    {
        // Create VR Rig
        GameObject rigObj = new GameObject("VR Rig");
        HUIXVRRig rig = rigObj.AddComponent<HUIXVRRig>();
        
        // VR Manager is automatically created
        // Head tracking is automatically enabled
        // Input system is ready to use
    }
}
```

---

## 📖 Documentation

### Components Overview

#### HUIXVRManager
The core manager that controls the entire VR system.

```csharp
// Get the manager instance
HUIXVRManager manager = HUIXVRManager.Instance;

// Enable/disable VR mode
manager.EnableVRMode();
manager.DisableVRMode();
manager.ToggleVRMode();

// Recenter the view
manager.Recenter();

// Change headset profile
manager.SetHeadsetProfile(myProfile);

// Adjust IPD
manager.SetIPD(65f); // 65mm
```

#### HUIXVRCamera
Handles stereoscopic rendering.

```csharp
HUIXVRCamera vrCamera = FindObjectOfType<HUIXVRCamera>();

// Enable stereo rendering
vrCamera.EnableStereoRendering(true);

// Adjust eye separation
vrCamera.EyeSeparation = 0.064f; // 64mm

// Get gaze direction
Ray gazeRay = vrCamera.GetGazeRay();
Vector3 gazePoint = vrCamera.GetGazePoint(100f);
```

#### HUIXHeadTracker
Manages device sensor-based head tracking.

```csharp
HUIXHeadTracker tracker = FindObjectOfType<HUIXHeadTracker>();

// Enable/disable tracking
tracker.EnableTracking(true);

// Recenter
tracker.Recenter();

// Get rotation data
Quaternion rotation = tracker.CurrentRotation;
Vector3 euler = tracker.GetEulerAngles();
Vector3 forward = tracker.GetForward();

// Change tracking mode
tracker.SetTrackingMode(HUIXHeadTracker.TrackingMode.GyroscopeWithFilter);
```

#### HUIXInputManager
Handles all VR input (gaze, triggers, etc.)

```csharp
// Subscribe to input events
HUIXInputManager.OnTriggerDown += () => Debug.Log("Trigger pressed!");
HUIXInputManager.OnGazeSelect += (obj) => Debug.Log($"Selected: {obj.name}");
HUIXInputManager.OnBackButton += () => Application.Quit();

// Get current gaze target
HUIXInputManager input = FindObjectOfType<HUIXInputManager>();
GameObject gazedObject = input.CurrentGazedObject;
float dwellProgress = input.GazeDwellProgress;
```

### Creating Interactable Objects

Implement the `IHUIXInteractable` interface:

```csharp
using UnityEngine;
using HUIX.PhoneVR;

public class MyInteractable : MonoBehaviour, IHUIXInteractable
{
    public void OnGazeEnter()
    {
        // User started looking at this object
        GetComponent<Renderer>().material.color = Color.yellow;
    }

    public void OnGazeExit()
    {
        // User stopped looking at this object
        GetComponent<Renderer>().material.color = Color.white;
    }

    public void OnSelect()
    {
        // User selected this object (trigger/tap)
        Debug.Log("Selected!");
    }
}
```

### Using VR UI Components

```csharp
using HUIX.PhoneVR.UI;

// VR Button
HUIXVRButton button = GetComponent<HUIXVRButton>();
button.AddClickListener(() => Debug.Log("Button clicked!"));
button.Interactable = true;

// VR Slider
HUIXVRSlider slider = GetComponent<HUIXVRSlider>();
slider.AddValueChangedListener((value) => Debug.Log($"Value: {value}"));
slider.Value = 0.5f;
```

### Headset Profiles

Create custom profiles for different VR headsets:

1. **Right-click** in Project window
2. Select **Create > HUIX > Phone VR > Headset Profile**
3. Configure the profile settings:
   - FOV (Field of View)
   - IPD (Inter-Pupillary Distance)
   - Lens distortion coefficients
   - Chromatic aberration correction

```csharp
// Create profile programmatically
HeadsetProfile profile = ScriptableObject.CreateInstance<HeadsetProfile>();
profile.ProfileName = "My Headset";
profile.FieldOfView = 100f;
profile.IPD = 64f;
profile.DistortionK1 = 0.34f;
profile.DistortionK2 = 0.55f;

// Apply to manager
HUIXVRManager.Instance.SetHeadsetProfile(profile);
```

---

## 🎮 Controls

### Default Controls

| Input | Action |
|-------|--------|
| Move Phone | Look around |
| Screen Tap | Select / Trigger |
| Double Tap | Recenter view |
| Back Button | Back / Exit |

### Editor Controls (Simulation)

| Input | Action |
|-------|--------|
| Right Mouse + Move | Look around |
| Arrow Keys | Look around |
| Left Click | Select / Trigger |
| R Key | Recenter |
| V Key | Toggle VR mode |

---

## ⚙️ Configuration

### Project Settings

For best results, configure your project:

1. **Player Settings > Resolution**
   - Default Orientation: Landscape Left
   - Auto Rotation: Landscape only

2. **Quality Settings**
   - V Sync Count: Don't Sync
   - Anti-Aliasing: 2x or 4x

3. **Player Settings > Other**
   - Target Frame Rate: 60

### Build Settings

- **Android**: Minimum API Level 21+
- **iOS**: Minimum iOS 11.0+

---

## 📱 Supported Devices

### Headsets
- Google Cardboard and compatible
- Generic phone VR headsets
- Custom headsets (with profile)

### Phones
- Android 5.0+ with gyroscope
- iOS 11.0+ with gyroscope

---

## 🔧 Troubleshooting

### Head tracking not working
- Ensure device has a gyroscope
- Check that `EnableTracking(true)` is called
- Try `Recenter()` if orientation seems wrong

### Distortion looks wrong
- Adjust headset profile distortion values
- Try a different preset profile
- Ensure screen size matches headset

### Low frame rate
- Enable Adaptive Quality
- Reduce render scale
- Simplify scene geometry

---

## 📄 License

MIT License - See [LICENSE.md](LICENSE.md) for details.

---

## 🤝 Support

- **Issues**: [GitHub Issues](https://github.com/huix/phone-vr-sdk/issues)
- **Email**: support@huix.dev
- **Discord**: [HUIX Community](https://discord.gg/huix)

---

## 🙏 Credits

Developed by **HUIX**

Special thanks to the VR development community.

---

<p align="center">
  Made with ❤️ for the VR community
</p>

