using UnityEngine;
using UnityEngine.Video;
using Unity.Profiling;

namespace ARFoundationReplay
{
    /// <summary>
    /// Replay video file with timeline metadata
    /// and mock ARFoundation subsystems without any changes
    /// </summary>
    internal sealed class ARReplay : System.IDisposable
    {
        private static readonly ProfilerMarker kDeserializeMarker = new("ARReplay.Deserialize");
        private static ARReplay _sharedInstance = null;

        public static bool TryGetReplay(out ARReplay replay)
        {
            if (!Application.isPlaying || _sharedInstance == null)
            {
                replay = null;
                return false;
            }
            replay = _sharedInstance;
            return replay.DidUpdateThisFrame;
        }

        private readonly VideoPlayer _video;
        private readonly MetadataPlayer _metadata;
        private long _lastFrame = long.MinValue;

        /// <summary>
        /// Whether the video frame was updated in this frame
        /// </summary>
        /// <value>True when updated in this frame</value>
        public bool DidUpdateThisFrame { get; private set; } = false;

        /// <summary>
        /// Whether the video frame was looped in this frame
        /// </summary>
        /// <value>True when looped in this frame</value>
        public bool DidLoopThisFrame { get; private set; } = false;
        public Texture Texture => _video.texture;
        public FrameMetadata Metadata { get; private set; }

        public ARReplay(ARFoundationReplaySettings settings)
        {
            if (_sharedInstance != null)
            {
                throw new System.InvalidOperationException("ARReplay is already initialized");
            }

            string path = settings.GetRecordPath();
            _video = CreateVideoPlayer(path);
            _metadata = new MetadataPlayer(path);

            _sharedInstance = this;
        }

        public void Dispose()
        {
            _metadata?.Dispose();
            if (_video != null)
            {
                if (Application.isEditor)
                {
                    Object.DestroyImmediate(_video);
                }
                else
                {
                    Object.Destroy(_video);
                }
            }
            _sharedInstance = null;
        }

        public void Update()
        {
            if (!_video.isPlaying)
            {
                DidUpdateThisFrame = false;
                DidLoopThisFrame = false;
                return;
            }

            // Skip the same frame
            long frame = _video.frame;
            if (frame == _lastFrame)
            {
                DidUpdateThisFrame = false;
                DidLoopThisFrame = false;
                return;
            }

            double time = _video.time;
            var metadata = _metadata.PeekMetadata(time);
            if (metadata.IsEmpty)
            {
                Debug.LogWarning($"Metadata not found, time:{time}");
                DidUpdateThisFrame = false;
                return;
            }

            kDeserializeMarker.Begin();
            Metadata = FrameMetadata.Deserialize(metadata);
            kDeserializeMarker.End();

            DidUpdateThisFrame = true;
            DidLoopThisFrame = _lastFrame > frame;
            _lastFrame = _video.frame;

            // Forward to the input native plugin
            ARReplayInputSubsystem.Update(Metadata);
        }

        private static VideoPlayer CreateVideoPlayer(string path)
        {
            var gameObject = new GameObject(typeof(ARReplay).ToString());

            var player = gameObject.AddComponent<VideoPlayer>();
            player.source = VideoSource.Url;
            player.url = $"file://{path}";
            player.playOnAwake = true;
            player.isLooping = true;
            player.skipOnDrop = true;
            player.renderMode = VideoRenderMode.APIOnly;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.SetDirectAudioMute(0, true);
            player.playbackSpeed = 1;

            return player;
        }
    }
}
