# HUIX VR SDK - Phone

**Turn your phone into a VR headset. One script. That's it.**

No demos. No complex setup. No build errors. Just VR.

---

## 🚀 Quick Start

### Step 1: Import the SDK
Copy the `HUIX-VR-SDK` folder into your Unity project's `Assets` folder.

### Step 2: Add VR to Your Camera
Select your **Main Camera** → Add Component → **HUIX VR Camera**

Or use the menu: `HUIX VR > Add VR to Main Camera`

### Step 3: Press Play
Done. You now have VR.

---

## 📱 What It Does

- **Stereo Rendering** - Split screen for left/right eyes (same as Google Cardboard)
- **Head Tracking** - Uses phone gyroscope for looking around
- **Lens Distortion** - Barrel distortion correction for VR lenses
- **Cardboard Compatible** - Works with any Cardboard-style VR viewer

---

## ⚙️ Settings

| Setting | Description | Default |
|---------|-------------|---------|
| VR Enabled | Turn VR mode on/off | true |
| IPD | Inter-pupillary distance (eye separation) | 0.064m |
| Screen To Lens | Distance from screen to lens | 0.042m |
| Distortion K1/K2 | Lens distortion coefficients | 0.441, 0.156 |
| Field Of View | Camera FOV in degrees | 95° |
| Enable Head Tracking | Use gyroscope for head movement | true |
| Show Divider | Black line between eyes | true |

---

## 🎮 Runtime API

```csharp
// Get reference
HUIXVRCamera vr = GetComponent<HUIXVRCamera>();

// Recenter the view
vr.Recenter();

// Toggle VR on/off
vr.ToggleVR();

// Apply preset profiles
vr.ApplyCardboardV1Profile();
vr.ApplyCardboardV2Profile();

// Access eye cameras
Transform leftEye = vr.GetLeftEye();
Transform rightEye = vr.GetRightEye();
```

---

## 📋 Profiles

### Cardboard V1 (Original)
- IPD: 0.06m
- Screen to Lens: 0.042m
- K1: 0.441, K2: 0.156
- FOV: 90°

### Cardboard V2 (2015+)
- IPD: 0.064m
- Screen to Lens: 0.039m
- K1: 0.34, K2: 0.55
- FOV: 95°

Apply via menu: `HUIX VR > Apply Cardboard V1/V2 Profile`

---

## 📦 Files

```
HUIX-VR-SDK/
├── Runtime/
│   ├── Scripts/
│   │   ├── HUIXVRCamera.cs      ← THE ONE SCRIPT
│   │   └── HUIXDeviceProfile.cs  ← Optional profiles
│   └── Shaders/
│       └── HUIXLensDistortion.shader
├── Editor/
│   └── HUIXVRSetup.cs           ← Editor helpers
├── package.json
└── README.md
```

---

## ✅ Requirements

- Unity 2019.4 or later
- Device with gyroscope (any modern phone)
- Any Cardboard-compatible VR viewer

---

## 🔧 Build Settings

For Android:
1. File > Build Settings > Android
2. Player Settings > Other Settings:
   - Auto Graphics API: ✓
   - Minimum API Level: 24+
3. Build and Run

For iOS:
1. File > Build Settings > iOS  
2. Build and open in Xcode
3. Run on device

---

## License

MIT License - Use it however you want.

Based on lens distortion algorithms from Google Cardboard SDK (Apache 2.0).

