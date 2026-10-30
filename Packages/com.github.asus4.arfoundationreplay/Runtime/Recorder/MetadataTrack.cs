using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace ARFoundationReplay
{
    /// <summary>
    /// The timed metadata track ('mebx') of a QuickTime file.
    /// </summary>
    /// <remarks>
    /// QuickTime File Format specification:
    /// - Timed metadata media: https://developer.apple.com/documentation/quicktime-file-format/timed_metadata_media
    /// - Sample table atom: https://developer.apple.com/documentation/quicktime-file-format/sample_table_atom
    /// </remarks>
    internal sealed class MetadataTrack
    {
        private readonly long[] _offsets;
        private readonly int[] _sizes;
        private readonly uint _localKeyId;

        public int MaxSampleSize { get; }

        private MetadataTrack(long[] offsets, int[] sizes, uint localKeyId)
        {
            _offsets = offsets;
            _sizes = sizes;
            _localKeyId = localKeyId;
            foreach (int size in sizes)
            {
                MaxSampleSize = Math.Max(MaxSampleSize, size);
            }
        }

        /// <param name="identifier">"keySpace/key", like "mdta/com.example.key"</param>
        /// <returns>null if no track declares the identifier</returns>
        public static MetadataTrack Read(Stream stream, string identifier, out bool hasAudioTrack)
        {
            hasAudioTrack = false;
            byte[] moov = ReadMoov(stream);
            if (moov == null)
            {
                return null;
            }

            uint keySpace = FourCC(identifier.Substring(0, 4));
            byte[] key = Encoding.UTF8.GetBytes(identifier.Substring(5));

            MetadataTrack track = null;
            int position = 0;
            while (NextBox(moov, ref position, out uint type, out var trak))
            {
                if (type != FourCC("trak")
                    || !TryFindBox(trak, "mdia", out var mdia)
                    || !TryFindBox(mdia, "hdlr", out var hdlr)
                    || hdlr.Length < 12)
                {
                    continue;
                }

                // hdlr: version and flags, component type, then the handler type
                uint handler = ReadUInt32(hdlr, 8);
                if (handler == FourCC("soun"))
                {
                    hasAudioTrack = true;
                }
                else if (handler == FourCC("meta") && track == null)
                {
                    track = ReadSampleTable(mdia, keySpace, key);
                }
            }
            return track;
        }

        /// <param name="stream">The file that the track was read from</param>
        /// <param name="buffer">At least <see cref="MaxSampleSize"/> bytes</param>
        /// <returns>A slice of the buffer, or empty if there is no such sample</returns>
        public ReadOnlySpan<byte> ReadSample(Stream stream, int index, byte[] buffer)
        {
            if ((uint)index >= (uint)_sizes.Length)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            int size = _sizes[index];
            stream.Position = _offsets[index];
            if (!ReadFully(stream, buffer, 0, size))
            {
                return ReadOnlySpan<byte>.Empty;
            }

            // Each item in a sample is a box typed by its local key ID
            ReadOnlySpan<byte> sample = new(buffer, 0, size);
            int position = 0;
            while (NextBox(sample, ref position, out uint id, out var value))
            {
                if (id == _localKeyId)
                {
                    return value;
                }
            }
            return ReadOnlySpan<byte>.Empty;
        }

        private static MetadataTrack ReadSampleTable(ReadOnlySpan<byte> mdia, uint keySpace, byte[] key)
        {
            if (!TryFindBox(mdia, "minf", out var minf)
                || !TryFindBox(minf, "stbl", out var stbl)
                || !TryFindBox(stbl, "stsd", out var stsd)
                || !TryFindLocalKeyId(stsd, keySpace, key, out uint localKeyId)
                || !TryFindBox(stbl, "stsc", out var stsc)
                || !TryFindBox(stbl, "stsz", out var stsz))
            {
                return null;
            }
            bool largeOffsets = !TryFindBox(stbl, "stco", out var chunkOffsets);
            if (largeOffsets && !TryFindBox(stbl, "co64", out chunkOffsets))
            {
                return null;
            }

            // stsz: version and flags, a fixed sample size or 0, the sample count, then each size if not fixed
            int fixedSize = (int)ReadUInt32(stsz, 4);
            int count = (int)ReadUInt32(stsz, 8);
            var sizes = new int[count];
            for (int i = 0; i < count; i++)
            {
                sizes[i] = fixedSize != 0 ? fixedSize : (int)ReadUInt32(stsz, 12 + i * 4);
            }

            // stsc: runs of chunks with the same number of samples, from a 1-based first chunk.
            // The samples of a chunk are stored back to back from its stco/co64 offset.
            var offsets = new long[count];
            int chunkCount = (int)ReadUInt32(chunkOffsets, 4);
            int entries = (int)ReadUInt32(stsc, 4);
            int sample = 0;
            for (int entry = 0; entry < entries; entry++)
            {
                int firstChunk = (int)ReadUInt32(stsc, 8 + entry * 12) - 1;
                int samplesPerChunk = (int)ReadUInt32(stsc, 12 + entry * 12);
                int endChunk = entry + 1 < entries ? (int)ReadUInt32(stsc, 20 + entry * 12) - 1 : chunkCount;
                for (int chunk = firstChunk; chunk < endChunk; chunk++)
                {
                    long offset = largeOffsets
                        ? (long)BinaryPrimitives.ReadUInt64BigEndian(chunkOffsets.Slice(8 + chunk * 8))
                        : ReadUInt32(chunkOffsets, 8 + chunk * 4);
                    for (int i = 0; i < samplesPerChunk && sample < count; i++)
                    {
                        offsets[sample] = offset;
                        offset += sizes[sample];
                        sample++;
                    }
                }
            }
            return new MetadataTrack(offsets, sizes, localKeyId);
        }

        // stsd: version and flags, entry count, then the entries. A 'mebx' entry has 8 bytes of
        // reserved and data reference index, then a 'keys' box with a box per local key ID.
        // Each declares its key in a 'keyd' box: the key space, then the key.
        private static bool TryFindLocalKeyId(ReadOnlySpan<byte> stsd, uint keySpace, byte[] key, out uint localKeyId)
        {
            int position = 8;
            while (NextBox(stsd, ref position, out uint format, out var entry))
            {
                if (format != FourCC("mebx")
                    || entry.Length < 8
                    || !TryFindBox(entry.Slice(8), "keys", out var keys))
                {
                    continue;
                }
                int keyPosition = 0;
                while (NextBox(keys, ref keyPosition, out uint id, out var declaration))
                {
                    if (TryFindBox(declaration, "keyd", out var keyd)
                        && keyd.Length >= 4
                        && ReadUInt32(keyd, 0) == keySpace
                        && keyd.Slice(4).SequenceEqual(key))
                    {
                        localKeyId = id;
                        return true;
                    }
                }
            }
            localKeyId = 0;
            return false;
        }

        private static byte[] ReadMoov(Stream stream)
        {
            var header = new byte[16];
            long position = 0;
            while (position < stream.Length)
            {
                long available = stream.Length - position;
                int headerLength = (int)Math.Min(header.Length, available);
                stream.Position = position;
                if (!ReadFully(stream, header, 0, headerLength)
                    || !TryReadHeader(new ReadOnlySpan<byte>(header, 0, headerLength), available,
                        out uint type, out int headerSize, out long size))
                {
                    return null;
                }

                if (type == FourCC("moov"))
                {
                    var moov = new byte[size - headerSize];
                    stream.Position = position + headerSize;
                    return ReadFully(stream, moov, 0, moov.Length) ? moov : null;
                }
                position += size;
            }
            return null;
        }

        private static bool TryFindBox(ReadOnlySpan<byte> data, string type, out ReadOnlySpan<byte> content)
        {
            uint target = FourCC(type);
            int position = 0;
            while (NextBox(data, ref position, out uint boxType, out content))
            {
                if (boxType == target)
                {
                    return true;
                }
            }
            content = default;
            return false;
        }

        private static bool NextBox(ReadOnlySpan<byte> data, ref int position, out uint type, out ReadOnlySpan<byte> content)
        {
            var rest = data.Slice(Math.Min(position, data.Length));
            if (!TryReadHeader(rest, rest.Length, out type, out int headerSize, out long size))
            {
                content = default;
                return false;
            }
            content = rest.Slice(headerSize, (int)size - headerSize);
            position += (int)size;
            return true;
        }

        private static bool TryReadHeader(ReadOnlySpan<byte> header, long available,
            out uint type, out int headerSize, out long size)
        {
            type = 0;
            headerSize = 8;
            size = 0;
            if (header.Length < 8)
            {
                return false;
            }
            size = ReadUInt32(header, 0);
            type = ReadUInt32(header, 4);
            if (size == 1)
            {
                // 64-bit size
                if (header.Length < 16)
                {
                    return false;
                }
                size = (long)BinaryPrimitives.ReadUInt64BigEndian(header.Slice(8));
                headerSize = 16;
            }
            else if (size == 0)
            {
                // Extends to the end
                size = available;
            }
            return size >= headerSize && size <= available;
        }

        private static bool ReadFully(Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int read = stream.Read(buffer, offset, count);
                if (read <= 0)
                {
                    return false;
                }
                offset += read;
                count -= read;
            }
            return true;
        }

        private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
            => BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset));

        private static uint FourCC(string code)
            => (uint)code[0] << 24 | (uint)code[1] << 16 | (uint)code[2] << 8 | code[3];
    }
}
