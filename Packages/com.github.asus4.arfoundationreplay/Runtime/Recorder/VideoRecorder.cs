using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace ARFoundationReplay
{
    public sealed class VideoRecorder : IDisposable
    {
        private RenderTexture _source = null;
        private RenderTexture _buffer;
        private readonly MetadataQueue _metadataQueue = new();

        public bool IsRecording { get; private set; }
        public int FrameRate { get; private set; } = -1;

        public VideoRecorder(RenderTexture source)
        {
            ChangeSource(source);
        }

        public void Dispose()
        {
            if (IsRecording)
            {
                EndRecording();
            }
            UnityEngine.Object.Destroy(_buffer);
        }

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

            // Get pixel buffer
            var data = request.GetData<byte>(0);
            var ptr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);

            var (time, metadata) = _metadataQueue.Dequeue();
            if (metadata.IsCreated)
            {
                var metadataPtr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(metadata);
                Avfi.AppendFrame(ptr, (uint)data.Length, metadataPtr, (uint)metadata.Length, time);
                metadata.Dispose();
            }
            else
            {
                Avfi.AppendFrame(ptr, (uint)data.Length, IntPtr.Zero, 0, time);
            }
        }

    }

} // namespace ARFoundationReplay
