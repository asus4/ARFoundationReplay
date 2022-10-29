using UnityEngine;
using UnityEngine.Video;

namespace ARFoundationReplay
{
    /// <summary>
    /// Replay video file with timeline metadata.
    /// </summary>
    internal sealed class ARReplay : System.IDisposable
    {
        public static ARReplay Current { get; private set; } = null;

        private readonly VideoPlayer _video;
        private readonly MetadataPlayer _metadata;
        private long _lastFrame = long.MinValue;
        private ARReplayInputSubsystem _input;

        public bool DidUpdateThisFrame { get; private set; } = false;
        public Texture Texture => _video.texture;
        public Packet Packet { get; private set; }

        public ARReplay(ARFoundationReplaySettings settings)
        {
            if (Current != null)
            {
                throw new System.InvalidOperationException("ARReplay is already initialized");
            }

            string path = settings.GetRecordPath();
            _video = CreateVideoPlayer(path);
            _metadata = new MetadataPlayer(path);
            _input = new ARReplayInputSubsystem();

            Current = this;
        }

        public void Dispose()
        {
            _input.Dispose();
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
            Current = null;
        }

        public void Update()
        {
            if (!_video.isPlaying)
            {
                DidUpdateThisFrame = false;
                return;
            }

            // Skip the same frame
            long frame = _video.frame;
            if (frame == _lastFrame)
            {
                DidUpdateThisFrame = false;
                return;
            }

            double time = _video.time;
            var metadata = _metadata.PeekMetadata(time).AsReadOnlySpan();
            Packet = Packet.Deserialize(metadata);

            _lastFrame = _video.frame;
            DidUpdateThisFrame = true;

            _input.Update(Packet);
        }

        static VideoPlayer CreateVideoPlayer(string path)
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
