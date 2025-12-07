# HUIX Phone VR SDK - Quick Start Guide

## Overview

This guide will help you get started with HUIX Phone VR SDK in just a few minutes.

## Prerequisites

- Unity 2021.3 or later
- Android or iOS build support installed
- A smartphone with gyroscope for testing

## Step 1: Install the SDK

### Via Package Manager
1. Open **Window > Package Manager**
2. Click **+** > **Add package from git URL**
3. Enter the repository URL

### Manual Installation
1. Copy the SDK folder to your project's `Packages` directory

## Step 2: Create Your First VR Scene

### Option A: Quick Setup (Recommended)
1. Create a new scene or open an existing one
2. Go to **HUIX > Phone VR > Quick Setup**
3. A complete VR rig is automatically created!

### Option B: Setup Wizard
1. Go to **HUIX > Phone VR > Setup Wizard**
2. Follow the wizard steps
3. Click **Setup VR Scene**

### Option C: Manual Setup
```csharp
// Add this script to any GameObject
using UnityEngine;
using HUIX.PhoneVR;

public class SetupVR : MonoBehaviour
{
    void Start()
    {
        // Create VR Rig - this sets up everything
        GameObject rig = new GameObject("HUIX VR Rig");
        rig.AddComponent<HUIXVRRig>();
    }
}
```

## Step 3: Test in Editor

1. Press **Play**
2. Hold **Right Mouse Button** and move mouse to look around
3. **Left Click** to select/trigger
4. Press **R** to recenter the view

## Step 4: Add Interactive Objects

Make any object interactive with gaze:

```csharp
using UnityEngine;
using HUIX.PhoneVR;

public class InteractiveObject : MonoBehaviour, IHUIXInteractable
{
    private Renderer rend;
    
    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    public void OnGazeEnter()
    {
        rend.material.color = Color.yellow;
    }

    public void OnGazeExit()
    {
        rend.material.color = Color.white;
    }

    public void OnSelect()
    {
        Debug.Log("Object selected!");
        // Add your interaction logic here
    }
}
```

## Step 5: Build for Mobile

### Android
1. Go to **File > Build Settings**
2. Select **Android** platform
3. Click **Switch Platform**
4. Configure in **Player Settings**:
   - Default Orientation: **Landscape Left**
   - Disable **Auto Rotation** for Portrait
5. Click **Build and Run**

### iOS
1. Go to **File > Build Settings**
2. Select **iOS** platform
3. Click **Switch Platform**
4. Configure in **Player Settings**:
   - Default Orientation: **Landscape Left**
   - Minimum iOS Version: **11.0**
5. Click **Build**
6. Open in Xcode and deploy to device

## Step 6: Test on Device

1. Install the app on your phone
2. Insert phone into VR headset
3. Look around to test head tracking
4. Tap screen to select objects

## Next Steps

- Explore the **Demo Scene** in Samples
- Create custom **Headset Profiles**
- Add **VR UI** components
- Implement **Teleportation** for locomotion
- Read the full documentation

## Troubleshooting

### Nothing shows in VR mode
- Ensure cameras are enabled
- Check that HUIXVRManager is initialized

### Head tracking is jittery
- Try different TrackingMode settings
- Adjust smoothing values

### Objects not responding to gaze
- Ensure objects have Colliders
- Check that they implement IHUIXInteractable
- Verify LayerMask settings in InputManager

## Resources

- [Full Documentation](../README.md)
- [API Reference](API.md)
- [Sample Code](../Samples~/DemoScene/)

---

Need help? Contact support@huix.dev

