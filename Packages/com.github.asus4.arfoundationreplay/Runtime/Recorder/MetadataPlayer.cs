using System;

namespace ARFoundationReplay
{
    public class MetadataPlayer : IDisposable
    {
        private byte[] _buffer;

        public MetadataPlayer(string path)
        {
            Avfi.LoadMetadata(path);
            // Get max size of metadata
            uint size = Avfi.GetBufferSize();
            _buffer = new byte[size];
        }

        public void Dispose()
        {
            Avfi.UnloadMetadata();
        }

        public unsafe ReadOnlySpan<byte> PeekMetadata(double time)
        {
            fixed (byte* ptr = _buffer)
            {
                uint size = Avfi.PeekMetadata(time, ptr);
                if (size == 0)
                {
                    return ReadOnlySpan<byte>.Empty;
                }
                return new ReadOnlySpan<byte>(ptr, (int)size);
            }
        }
    }
}
