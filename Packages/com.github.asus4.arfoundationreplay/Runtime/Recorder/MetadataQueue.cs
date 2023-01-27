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
        private double _last;

        public int Count => _times.Count;

        public MetadataQueue(int targetFrameRate = 60)
        {
            _targetFrameRate = targetFrameRate;
        }

        public void Dispose()
        {
            Clear();
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
        }

        public (double, NativeArray<byte>) Dequeue()
        {
            return (_times.Dequeue(), _buffers.Dequeue());
        }

        public unsafe bool TryEnqueueNow(ReadOnlySpan<byte> metadata)
        {
            // Copy native array
            var buffer = metadata.CopyToNativeArray(Allocator.Persistent);

            var time = Time.unscaledTimeAsDouble - _start;

            if (_start == 0)
            {
                _times.Enqueue(0);
                _buffers.Enqueue(buffer);
                _start = Time.unscaledTimeAsDouble;
                _last = 0;
                return true;
            }
            else
            {
                // Reject it if it falls into the same frame.
                if ((int)(time * _targetFrameRate) == (int)(_last * _targetFrameRate))
                {
                    buffer.Dispose();
                    return false;
                }

                _times.Enqueue(time);
                _buffers.Enqueue(buffer);
                _last = time;
                return true;
            }
        }
    }
}
