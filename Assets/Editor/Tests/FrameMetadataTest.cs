using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace ARFoundationReplay.Tests
{
    public class FrameMetadata
    {
        [Test]
        public void XRCameraFrameSerializeTest()
        {
            XRCameraFrame input = new XRCameraFrame(
                timestamp: 1234567890,
                averageBrightness: 0.1f,
                averageColorTemperature: 0.2f,
                colorCorrection: new Color(0.3f, 0.4f, 0.5f, 0.6f),
                projectionMatrix: new Matrix4x4(
                    new Vector4(0.1f, 0.2f, 0.3f, 0.4f),
                    new Vector4(0.5f, 0.6f, 0.7f, 0.8f),
                    new Vector4(0.9f, 1.0f, 1.1f, 1.2f),
                    new Vector4(1.3f, 1.4f, 1.5f, 1.6f)),
                displayMatrix: new Matrix4x4(
                    new Vector4(0.1f, 0.2f, 0.3f, 0.4f),
                    new Vector4(0.5f, 0.6f, 0.7f, 0.8f),
                    new Vector4(0.9f, 1.0f, 1.1f, 1.2f),
                    new Vector4(1.3f, 1.4f, 1.5f, 1.6f)),
                trackingState: TrackingState.Tracking,
                nativePtr: System.IntPtr.Zero,
                properties: XRCameraFrameProperties.Timestamp
                    | XRCameraFrameProperties.AverageBrightness
                    | XRCameraFrameProperties.AverageColorTemperature
                    | XRCameraFrameProperties.ColorCorrection
                    | XRCameraFrameProperties.AverageIntensityInLumens,
                averageIntensityInLumens: 0.7f,
                exposureDuration: 0.8f,
                exposureOffset: 0.9f,
                mainLightIntensityInLumens: 1.0f,
                mainLightColor: new Color(1.1f, 1.2f, 1.3f, 1.4f),
                mainLightDirection: new Vector3(1.5f, 1.6f, 1.7f),
                ambientSphericalHarmonics: default,
                cameraGrain: default,
                noiseIntensity: 1.8f
            );

            byte[] bytes = input.ToByteArray();
            XRCameraFrame output = bytes.ToStruct<XRCameraFrame>();

            Assert.AreEqual(input, output);
        }
    }
}
