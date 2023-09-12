using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Video;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections.LowLevel.Unsafe;
using ARFoundationReplay;

[RequireComponent(typeof(VideoPlayer))]
public class MetadataPlayerTest : MonoBehaviour
{
    [SerializeField]
    private string _path;

    private MetadataPlayer _metadataPlayer;
    private VideoPlayer _videoPlayer;
    private long _lastFrame = long.MinValue;

    private void Awake()
    {
        Assert.IsTrue(UnsafeUtility.IsBlittable<XRCameraFrame>());
        // Assert.IsTrue(UnsafeUtility.IsBlittable<CameraPacket>());
        Debug.Log($"Size of XRCameraFrame: {UnsafeUtility.SizeOf<XRCameraFrame>()} bytes");
    }

    private void Start()
    {
        string path = Path.IsPathRooted(_path)
            ? _path
            : Path.Combine(Application.dataPath, "..", _path);
        _videoPlayer = GetComponent<VideoPlayer>();
        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.url = $"file://{path}";

        _metadataPlayer = new MetadataPlayer(path);
    }

    private void OnDestroy()
    {
        _metadataPlayer?.Dispose();
        _metadataPlayer = null;
    }

    private void Update()
    {
        if (!_videoPlayer.isPlaying)
        {
            return;
        }

        // Skip the same frame
        long frame = _videoPlayer.frame;
        if (frame == _lastFrame)
        {
            return;
        }

        double time = _videoPlayer.time;
        var rawMetadata = _metadataPlayer.PeekMetadata(time);
        var metadata = FrameMetadata.Deserialize(rawMetadata);
        Debug.Log($"f={frame}, t={time:0.00}, metadata={rawMetadata.Length} frame={metadata.camera}");

        _lastFrame = _videoPlayer.frame;
    }
}
