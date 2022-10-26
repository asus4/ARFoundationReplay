using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace ARRecorder
{
    internal sealed class MetadataQueue : System.IDisposable
    {
        private readonly Queue<double> _times = new();
        private readonly Queue<NativeArray<byte>> _buffers = new();

        private double _start;
        private double _last;

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

        public bool TryEnqueueNow(NativeArray<byte> metadata)
        {
            // Copy native array
            var buffer = new NativeArray<byte>(metadata, Allocator.Persistent);

            if (_start == 0)
            {
                _times.Enqueue(0);
                _buffers.Enqueue(buffer);
                _start = Time.timeAsDouble;
                _last = 0;
                return true;
            }
            else
            {
                var time = Time.timeAsDouble - _start;

                // Reject it if it falls into the same frame.
                if ((int)(time * 60) == (int)(_last * 60))
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
