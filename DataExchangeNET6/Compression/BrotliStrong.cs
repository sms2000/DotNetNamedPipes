using System.IO.Compression;
using Interfaces;

namespace Compression
{
    public class BrotliStrong : CompressorHelper, ICompressor
    {
        private const int CompressionLevel = 9;
        private const int Window = 24;

        public ICompressor.Compressor GetCompressorMethod()
        {
            return ICompressor.Compressor.Brotli;
        }

        public byte[] Deflate(byte[] src)
        {
            var memory = new byte[src.Length];
            if (BrotliEncoder.TryCompress(src, memory, out var compressedLength, CompressionLevel, Window))
            {
                var compressed = FillHeaderCompressed(memory, compressedLength, src.Length, GetCompressorMethod());
                return compressed;
            }
            else
            {
                var raw = FillHeaderRaw(memory, src.Length);
                return raw;
            }
        }

        public byte[] Inflate(byte[] src)
        {
            var data = ExtractCompressedOrRaw(src, out var type, out var uncompressedSize);
            if (type == GetCompressorMethod())
            {
                var memory = new byte[uncompressedSize];

                if (BrotliDecoder.TryDecompress(data, memory, out var decodedBytes))
                {
                    return memory;
                }
            }
            else if (type == ICompressor.Compressor.None)
            {
                return data;
            }

            throw new InvalidDataException("Failed to decompress the input");
        }
    }
}
