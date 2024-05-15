# AR Foundation Replay

**🚧 Work in progress 🏗️**  
PoC of Recording AR Foundation session into a single mp4 with a binary timeline track / Replaying it in Editor on AR Foundation without any extra settings.

<https://github.com/asus4/ARFoundationReplay/assets/357497/8e77ee45-6f2c-442e-a47a-f35f044b8181>

↓ Check out the [complete project](https://github.com/asus4/WorldEnsemble) utilizing ARFoundationReplay.

<https://github.com/asus4/WorldEnsemble/assets/357497/1ff03fd4-01cf-41a6-8aef-42e11f7a67a2>

## Tested platform

- Only iOS ARKit + macOS Editor for now. Android ARCore is planned.
- Tested with the latest Unity2022 LTS.
- Supported only URP. Built-in is not supported for now.

## Implemented Subsystems

- [x] XRSessionSubsystem
- [x] XRCameraSubsystem
- [ ] XRPointCloudSubsystem
- [x] XRPlaneSubsystem
- [ ] XRAnchorSubsystem
- [ ] XRRaycastSubsystem
- [ ] XRHumanBodySubsystem
- [ ] XREnvironmentProbeSubsystem
- [x] XRInputSubsystem
- [ ] XRImageTrackingSubsystem
- [ ] XRObjectTrackingSubsystem
- [ ] XRFaceSubsystem
- [x] XROcclusionSubsystem
- [ ] XRParticipantSubsystem
- [ ] XRMeshSubsystem
- ARCore Extensions
  - [x] Earth Manager
  - [ ] Cloud Anchors
  - [x] Streetscape Geometry

## How to use

- Install MemoryPack from NuGet using NuGetForUnity
  - `openupm add com.github-glitchenzo.nugetforunity`
  - Open Window from NuGet -> Manage NuGet Packages, Search "MemoryPack" and Press Install.
  ![screenshot](https://github.com/Cysharp/MemoryPack/assets/727159/599ff1ed-6cca-4724-be67-3edddb5e62ee)
- Add the following line to `Packages/manifest.json` to install AR Foundation Replay package via UPM:

  ```json
  "dependencies": {
      "com.github.asus4.arfoundationreplay": "https://github.com/asus4/ARFoundationReplay.git?path=Packages/com.github.asus4.arfoundationreplay#v0.2.0",
      ... other dependencies
  }
  ```

- Simulation on the Unity Editor is supported only on iOS. Open Build Settings and switch the platform to iOS.
  ![fig-switch-platform](https://github.com/asus4/WorldEnsemble/assets/357497/2bbcb90a-5f6f-4d2a-87a1-65db73f74a36)
- Activate AR Foundation Replay for Unity Editor:
  1. Open `Project Settings/XR Plug-in Management`.
  2. Select the PC tab in the XR Plug-in Management.
  3. Activate `AR Foundation Replay`
  ![activate-xr-plugin-for-editor](https://github.com/asus4/ARFoundationReplay/assets/357497/1889a55a-132a-4c31-8a98-c4b22f2bdf22)
- Put the ARRecordButton prefab into the Scene which is located at `Packages/com.github.asus4.arfoundationreplay/Prefabs/ARRecordButton.prefab`.
- Record AR on the device.
- You can change the replay file from `Project Settings/XR Plug-in Management/AR Foundation Replay.`
  ![replay-file](https://github.com/asus4/WorldEnsemble/assets/357497/35f3c0c9-fd72-4b0c-bf39-11132874a259)
- Play the AR in the Editor.

## ARCore Extensions Support

It supports replaying ARCore Geospatial. To test it, please use the [forked version of the arcore-unity-extensions](https://github.com/asus4/arcore-unity-extensions) and refer to the [ARFoundationReplayGeospatial](https://github.com/asus4/ARFoundationReplayGeospatial) example project.

## Dependencies

- [MemoryPack - MIT License](https://github.com/Cysharp/MemoryPack)
- [XR SDK - Unity Companion License](https://docs.unity3d.com/Manual/xr-sdk.html)
- [keijiro/Bibcam - Unlicense](https://github.com/keijiro/Bibcam)
- [keijiro/Avfi - Unlicense](https://github.com/keijiro/Avfi): Metadata recording system is modified from Avfi.
