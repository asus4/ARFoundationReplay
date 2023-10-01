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

1. Add following line to `Packages/manifest.json` to install AR Foundation Replay package via UPM:

```json
"dependencies": {
    "com.github.asus4.arfoundationreplay": "https://github.com/asus4/ARFoundationReplay.git?path=Packages/com.github.asus4.arfoundationreplay",
    ... other dependencies
}
```

2. Put the ARRecordButton prefab into the Scene which located at `Packages/com.github.asus4.arfoundationreplay/Prefabs/ARRecordButton.prefab`.
3. Record AR on the device.
4. Move your recorded file to the PC and set the file path at the `Project Settings/XR Plug-in Management/AR Foundation Replay` setting.
5. Play the AR in the Editor.

## Acknowledgement

- [XR SDK - Unity Companion License](https://docs.unity3d.com/Manual/xr-sdk.html)
- [keijiro/Bibcam - Unlicense](https://github.com/keijiro/Bibcam)
- [keijiro/Avfi - Unlicense](https://github.com/keijiro/Avfi): Metadata recording system is modified from Avfi.
