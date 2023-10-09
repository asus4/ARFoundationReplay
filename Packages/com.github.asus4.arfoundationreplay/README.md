# AR Foundation Replay

**Working in progress.**

PoC of Recording AR Foundation session into a single mp4 with a binary timeline track / Replaying it in Editor on AR Foundation without any extra settings.

## Supported platforms

- Only iOS ARKit + macOS Editor for now. Android ARCore is planned.
- Tested with the latest Unity2022 LTS
- Tested with URP.

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

## How to use

- Download [System.Runtime.CompilerServices.Unsafe](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.0.0) NuGet package and rename to zip to extract. Locate `lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll` to `Assets/Plugins/` directory.
- Add following line to `Packages/manifest.json` to install AR Foundation Replay package via UPM:

```json
"dependencies": {
    "com.cysharp.memorypack": "https://github.com/Cysharp/MemoryPack.git?path=src/MemoryPack.Unity/Assets/Plugins/MemoryPack#1.9.16",
    "com.github.asus4.arfoundationreplay": "https://github.com/asus4/ARFoundationReplay.git?path=Packages/com.github.asus4.arfoundationreplay",
    ... other dependencies
}
```

- Put the ARRecordButton prefab into the Scene which located at `Packages/com.github.asus4.arfoundationreplay/Prefabs/ARRecordButton.prefab`.
- Record AR on the device.
- Move your recorded file to the PC and set the file path at the `Project Settings/XR Plug-in Management/AR Foundation Replay` setting.
- Play the AR in the Editor.

## Dependencies

- [MemoryPack - MIT License](https://github.com/Cysharp/MemoryPack)
- [XR SDK - Unity Companion License](https://docs.unity3d.com/Manual/xr-sdk.html)
- [keijiro/Bibcam - Unlicense](https://github.com/keijiro/Bibcam)
- [keijiro/Avfi - Unlicense](https://github.com/keijiro/Avfi): Metadata recording system is modified from Avfi.
