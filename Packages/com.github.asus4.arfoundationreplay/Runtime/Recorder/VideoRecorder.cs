using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Collections.LowLevel.Unsafe;

namespace ARFoundationReplay
{
    /// <summary>
    /// Record video file with timeline metadata and an optional audio track.
    /// </summary>
    public sealed class VideoRecorder : IDisposable
    {
        private const int kMicrophoneSampleRate = 48000;

        private readonly MetadataQueue _metadataQueue;
        public readonly int targetFrameRate;
        public readonly AudioCaptureMode audioMode;

        private RenderTexture _source = null;
        private RenderTexture _buffer;
        private AudioOutputCapture _audioCapture;

        public bool IsRecording { get; private set; }

        /// <summary>
        /// The audio mode actually used by the current recording (None when the requested mode was unavailable).
        /// </summary>
        public AudioCaptureMode ActiveAudioMode { get; private set; } = AudioCaptureMode.None;

        public VideoRecorder(RenderTexture source, int targetFrameRate, AudioCaptureMode audioMode = AudioCaptureMode.None)
        {
            _source = source;
            _buffer = new RenderTexture(source.width, source.height, 0);

            this.targetFrameRate = targetFrameRate;
            this.audioMode = audioMode;
            _metadataQueue = new MetadataQueue(targetFrameRate);
        }

        public void Dispose()
        {
            if (IsRecording)
            {
                EndRecording();
            }
            _metadataQueue.Dispose();
            UnityEngine.Object.Destroy(_buffer);
        }

        /// <summary>
        /// Update metadata and record frame.
        /// </summary>
        /// <param name="metadata">Bytes of Metadata</param>
        public void Update(ReadOnlySpan<byte> metadata)
        {
            if (!IsRecording) { return; }
            if (!_metadataQueue.TryEnqueueNow(metadata)) { return; }

            Graphics.Blit(_source, _buffer);
            AsyncGPUReadback.Request(_buffer, 0, OnSourceReadback);
        }

        /// <summary>
        /// On iOS, warming up at the first time recording is recommended as it takes time.
        /// </summary>
        public void WarmUp()
        {
            var path = GetTemporaryFilePath();
            Avfi.StartRecording(path, _source.width, _source.height, (int)AudioCaptureMode.None, 0, 0);
            Avfi.EndRecording(false);
        }

        public void StartRecording()
        {
            var path = GetTemporaryFilePath();
            var mode = ResolveAudioMode(out int sampleRate, out int channels, out AudioListener listener);

            Avfi.StartRecording(path, _source.width, _source.height, (int)mode, sampleRate, channels);
            ActiveAudioMode = mode;
            // Start the clock right after the native recorder so the video and audio tracks share the same origin.
            _metadataQueue.Start(Time.realtimeSinceStartupAsDouble);

            if (mode == AudioCaptureMode.UnityAudioOutput)
            {
                _audioCapture = listener.gameObject.AddComponent<AudioOutputCapture>();
                _audioCapture.Begin();
            }
            IsRecording = true;
        }

        public void EndRecording()
        {
            if (_audioCapture != null)
            {
                _audioCapture.End();
                UnityEngine.Object.Destroy(_audioCapture);
                _audioCapture = null;
            }
            AsyncGPUReadback.WaitAllRequests();
            Avfi.EndRecording(true);
            IsRecording = false;
            ActiveAudioMode = AudioCaptureMode.None;
        }

        private AudioCaptureMode ResolveAudioMode(out int sampleRate, out int channels, out AudioListener listener)
        {
            sampleRate = 0;
            channels = 0;
            listener = null;

            switch (audioMode)
            {
                case AudioCaptureMode.NativeMicrophone:
                    if (!Avfi.HasMicrophonePermission())
                    {
                        Debug.LogWarning("VideoRecorder: Microphone permission is not granted, recording without audio.");
                        return AudioCaptureMode.None;
                    }
                    sampleRate = kMicrophoneSampleRate;
                    channels = 1;
                    return AudioCaptureMode.NativeMicrophone;

                case AudioCaptureMode.UnityAudioOutput:
                    listener = UnityEngine.Object.FindFirstObjectByType<AudioListener>();
                    if (listener == null)
                    {
                        Debug.LogWarning("VideoRecorder: No AudioListener found in the scene, recording without audio.");
                        return AudioCaptureMode.None;
                    }
                    sampleRate = AudioSettings.outputSampleRate;
                    channels = AudioOutputCapture.Channels;
                    return AudioCaptureMode.UnityAudioOutput;

                default:
                    return AudioCaptureMode.None;
            }
        }

        private static string GetTemporaryFilePath()
        {
            string dir = Application.platform == RuntimePlatform.IPhonePlayer
                ? Application.temporaryCachePath : ".";
            string sceneName = SceneManager.GetActiveScene().name;
            string fileName = $"Record_{sceneName}_{DateTime.Now:MMdd_HHmm_ss}.mp4";
            return $"{dir}/{fileName}";
        }

        private unsafe void OnSourceReadback(AsyncGPUReadbackRequest request)
        {
            if (!IsRecording)
            {
                return;
            }

            Assert.AreNotEqual(_metadataQueue.Count, 0);

            var (time, metadata) = _metadataQueue.Dequeue();
            if (!metadata.IsCreated)
            {
                return;
            }

            // Get pixel buffer
            using var pixelData = request.GetData<byte>(0);
            var pixelPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(pixelData);

            var metadataPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(metadata);
            Avfi.AppendFrame(pixelPtr, (uint)pixelData.Length, metadataPtr, (uint)metadata.Length, time);

            metadata.Dispose();
        }
    }

} // namespace ARFoundationReplay
