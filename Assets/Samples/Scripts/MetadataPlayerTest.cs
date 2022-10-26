using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Unity.Collections;
using ARRecorder;

[RequireComponent(typeof(VideoPlayer))]
public class MetadataPlayerTest : MonoBehaviour
{
    [SerializeField]
    private string _path;

    private MetadataPlayer _metadataPlayer;
    private VideoPlayer _videoPlayer;
    private long _lastFrame = long.MinValue;

    private void Start()
    {
        _videoPlayer = GetComponent<VideoPlayer>();
        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.url = $"file://{_path}";

        _metadataPlayer = new MetadataPlayer(_path);
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

        long frame = _videoPlayer.frame;
        if (frame == _lastFrame)
        {
            return;
        }

        double time = _videoPlayer.time;
        var metadata = _metadataPlayer.PeekMetadata(time).AsReadOnlySpan();
        var packet = Packet.Deserialize(metadata);
        Debug.Log($"f={frame}, t={time:0.00}, metadata={metadata.Length} frame={packet.cameraFrame}");

        _lastFrame = _videoPlayer.frame;
    }
}
