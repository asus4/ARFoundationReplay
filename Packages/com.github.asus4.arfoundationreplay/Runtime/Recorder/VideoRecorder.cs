using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace ARFoundationReplay
{
    /// <summary>
    /// Record video file with timeline metadata.
    /// </summary>
    public sealed class VideoRecorder : IDisposable
    {
        private readonly MetadataQueue _metadataQueue;
        public readonly int TargetFrameRate;

        private RenderTexture _source = null;
        private RenderTexture _buffer;
        private uint _frameCount = 0;

        public bool IsRecording { get; private set; }
        public bool FixedFrameRate { get; set; } = true;

        public VideoRecorder(RenderTexture source, int targetFrameRate)
        {
            ChangeSource(source);
            TargetFrameRate = targetFrameRate;
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
            Avfi.EndRecording();
            IsRecording = false;
        }

        private void ChangeSource(RenderTexture rt)
        {
            if (IsRecording)
            {
                Debug.LogError("Can't change the source while recording.");
                return;
            }

            if (_buffer != null)
            {
                UnityEngine.Object.Destroy(_buffer);
            }

            _source = rt;
            _buffer = new RenderTexture(rt.width, rt.height, 0);
        }

        private static string GetTemporaryFilePath()
        {
            string dir = Application.platform == RuntimePlatform.IPhonePlayer
                ? Application.temporaryCachePath : ".";
            string fileName = $"Record_{DateTime.Now:MMdd_HHmm_ss}.mp4";
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
            }

            // Override time as Unity 2022.2.1f1 doesn't support VFR video playback
            // https://issuetracker.unity3d.com/issues/video-created-with-avfoundation-framework-is-not-played-when-entering-the-play-mode
            if (FixedFrameRate)
            {
                time = _frameCount * (1.0 / TargetFrameRate);
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
