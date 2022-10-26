using System;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using DateTime = System.DateTime;
using IntPtr = System.IntPtr;

namespace ARRecorder
{
    public sealed class VideoRecorder : System.IDisposable
    {
        #region Editable attributes

        private RenderTexture _source = null;

        public RenderTexture Source
        {
            get => _source;
            set => ChangeSource(value);
        }

        #endregion

        #region Public properties and methods

        public bool IsRecording { get; private set; }

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
            Avfi.StartRecording(path, _source.width, _source.height);

            _metadataQueue.Clear();
            IsRecording = true;
        }

        public void EndRecording()
        {
            AsyncGPUReadback.WaitAllRequests();
            Avfi.EndRecording();
            IsRecording = false;
        }

        #endregion

        #region Private objects

        private RenderTexture _buffer;
        private readonly MetadataQueue _metadataQueue = new();

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

        #endregion

        #region Async GPU readback

        private unsafe void OnSourceReadback(AsyncGPUReadbackRequest request)
        {
            if (!IsRecording)
            {
                return;
            }

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

        #endregion
    }

} // namespace Avfi
