using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.Collections.LowLevel.Unsafe;

namespace ARFoundationReplay
{
    /// <summary>
    /// Record video file with timeline metadata.
    /// </summary>
    public sealed class VideoRecorder : IDisposable
    {
        private readonly MetadataQueue _metadataQueue;
        public readonly int targetFrameRate;

        private RenderTexture _source = null;
        private RenderTexture _buffer;
        private uint _frameCount = 0;

        public bool IsRecording { get; private set; }
        public bool FixedFrameRate { get; set; } = true;

        public VideoRecorder(RenderTexture source, int targetFrameRate)
        {
            _source = source;
            _buffer = new RenderTexture(source.width, source.height, 0);
            // _buffer = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linea);

            this.targetFrameRate = targetFrameRate;
            _metadataQueue = new MetadataQueue(targetFrameRate);
        }

        public void Dispose()
        {
            if (IsRecording)
            {
                EndRecording();
            }
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
            Avfi.StartRecording(path, _source.width, _source.height);
            Avfi.EndRecording(false);
        }

        public void StartRecording()
        {
            var path = GetTemporaryFilePath();
            _metadataQueue.Clear();
            Avfi.StartRecording(path, _source.width, _source.height);
            IsRecording = true;
            _frameCount = 0;
        }

        public void EndRecording()
        {
            AsyncGPUReadback.WaitAllRequests();
            Avfi.EndRecording(true);
            IsRecording = false;
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

            // Override time as Unity 2022.2.1f1 doesn't support VFR video playback
            // https://issuetracker.unity3d.com/issues/video-created-with-avfoundation-framework-is-not-played-when-entering-the-play-mode
            if (FixedFrameRate)
            {
                time = _frameCount * (1.0 / targetFrameRate);
            }

            // Get pixel buffer
            using var pixelData = request.GetData<byte>(0);
            var pixelPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(pixelData);

            var metadataPtr = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(metadata);
            Avfi.AppendFrame(pixelPtr, (uint)pixelData.Length, metadataPtr, (uint)metadata.Length, time);

            metadata.Dispose();

            _frameCount++;
        }

    }

} // namespace ARFoundationReplay
