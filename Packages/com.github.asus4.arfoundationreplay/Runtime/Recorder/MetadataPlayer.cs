using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ARFoundationReplay
{
    public class MetadataPlayer : IDisposable
    {
        private NativeArray<byte> _buffer;

        public MetadataPlayer(string path)
        {
            Avfi.LoadMetadata(path);
            uint size = Avfi.GetBufferSize();
            _buffer = new NativeArray<byte>((int)size, Allocator.Persistent);
        }

        public void Dispose()
        {
            Avfi.UnloadMetadata();
            _buffer.Dispose();
        }

        public unsafe NativeSlice<byte> PeekMetadata(double time)
        {
            var ptr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(_buffer);
            uint size = Avfi.PeekMetadata(time, ptr);
            return new NativeSlice<byte>(_buffer, 0, (int)size);
        }

        public unsafe ReadOnlySpan<byte> PeekMetadataAsSpan(double time)
        {
            var slice = PeekMetadata(time);
            if (slice.Length == 0)
            {
                return ReadOnlySpan<byte>.Empty;
            }
            return slice.AsReadOnlySpan();
        }
    }
}
