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
        private long _lastFrame = -1;

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
            _metadata = new MetadataPlayer(path);
            bool withAudio = settings.EnableAudio && _metadata.HasAudioTrack;
            _video = CreateVideoPlayer(path, withAudio);

            _sharedInstance = this;
        }

        public void Dispose()
        {
            _metadata?.Dispose();
            if (_video != null)
            {
                var gameObject = _video.gameObject;
                if (Application.isEditor)
                {
                    Object.DestroyImmediate(gameObject);
                }
                else
                {
                    Object.Destroy(gameObject);
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

            // Skip the same frame, and the time before the first one
            long frame = _video.frame;
            if (frame == _lastFrame || frame < 0)
            {
                DidUpdateThisFrame = false;
                DidLoopThisFrame = false;
                return;
            }

            // By the frame, not VideoPlayer.time: it is the frame divided by a constant frame rate,
            // which drifts from the recorded time after a dropped frame
            var metadata = _metadata.ReadFrame((int)frame);
            if (metadata.IsEmpty)
            {
                Debug.LogWarning($"Metadata not found, frame:{frame}");
                DidUpdateThisFrame = false;
                return;
            }

            kDeserializeMarker.Begin();
            Metadata = FrameMetadata.Deserialize(metadata);
            // Each frame has only the changes since the previous one,
            // so keep the changes of the frames that the video player skipped
            for (long i = frame - 1; i > _lastFrame; i--)
            {
                Metadata.MergeEarlier(_metadata.ReadFrame((int)i));
            }
            kDeserializeMarker.End();

            DidUpdateThisFrame = true;
            DidLoopThisFrame = _lastFrame > frame;
            _lastFrame = frame;

            // Forward to the input native plugin
            ARReplayInputSubsystem.Update(Metadata);
        }

        private static VideoPlayer CreateVideoPlayer(string path, bool withAudio)
        {
            var gameObject = new GameObject(typeof(ARReplay).ToString());

            var player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.Url;
            player.url = $"file://{path}";
            player.isLooping = true;
            player.skipOnDrop = true;
            player.renderMode = VideoRenderMode.APIOnly;
            player.playbackSpeed = 1;

            if (withAudio)
            {
                // Route the audio track through Unity's audio system
                var audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                player.audioOutputMode = VideoAudioOutputMode.AudioSource;
                player.controlledAudioTrackCount = 1;
                player.EnableAudioTrack(0, true);
                player.SetTargetAudioSource(0, audioSource);
            }
            else
            {
                player.audioOutputMode = VideoAudioOutputMode.None;
            }

            player.Play();
            return player;
        }
    }
}
