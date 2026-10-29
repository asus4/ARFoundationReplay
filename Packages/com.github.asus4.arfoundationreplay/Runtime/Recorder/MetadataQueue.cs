using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace ARFoundationReplay
{
    internal sealed class MetadataQueue : IDisposable
    {
        private readonly Queue<double> _times = new(4);
        private readonly Queue<NativeArray<byte>> _buffers = new(4);
        private readonly int _targetFrameRate;

        private double _start;
        private int _lastBucket = -1;

        public int Count => _times.Count;

        public MetadataQueue(int targetFrameRate = 60)
        {
            _targetFrameRate = targetFrameRate;
        }

        public void Dispose()
        {
            Clear();
        }

        /// <summary>
        /// Start the recording clock.
        /// Call right after the native recorder starts so the video and audio tracks share the same origin.
        /// </summary>
        public void Start(double now)
        {
            Clear();
            _start = now;
        }

        public void Clear()
        {
            while (_buffers.Count > 0)
            {
                _buffers.Dequeue().Dispose();
            }
            _buffers.Clear();
            _times.Clear();
            _start = 0;
            _lastBucket = -1;
        }

        public (double, NativeArray<byte>) Dequeue()
        {
            return (_times.Dequeue(), _buffers.Dequeue());
        }

        public bool TryEnqueueNow(ReadOnlySpan<byte> metadata)
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - _start;
            int bucket = (int)(elapsed * _targetFrameRate);

            // Reject it if it falls into the same frame slot as the previous one.
            if (bucket <= _lastBucket)
            {
                return false;
            }

            // Snap the time to the frame grid: constant frame duration, gaps when frames drop,
            // so the video stays aligned with the wall clock (and the audio track).
            _times.Enqueue(bucket / (double)_targetFrameRate);
            _buffers.Enqueue(metadata.CopyToNativeArray(Allocator.Persistent));
            _lastBucket = bucket;
            return true;
        }
    }
}
