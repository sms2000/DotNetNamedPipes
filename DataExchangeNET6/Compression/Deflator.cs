using System.IO.Compression;
using Interfaces;

namespace Compression
{
    public class Deflator : CompressorHelper, ICompressor
    {
        public ICompressor.Compressor GetCompressorMethod()
        {
            return ICompressor.Compressor.Deflator;
        }

        public byte[] Deflate(byte[] src)
        {
            using var uncompressedStream = new MemoryStream(src);
            using (var compressedStream = new MemoryStream())
            {
                using (var compressorStream = new DeflateStream(compressedStream, CompressionLevel.Fastest, true))
                {
                    uncompressedStream.CopyTo(compressorStream);
                }

                var memory = compressedStream.ToArray();
                if (src.Length > memory.Length)
                {
                    var compressed = FillHeaderCompressed(memory, memory.Length, src.Length, GetCompressorMethod());
                    return compressed;
                }
                else
                {
                    var raw = FillHeaderRaw(memory, src.Length);
                    return raw;
                }
            }
        }

        public byte[] Inflate(byte[] src)
        {
            var data = ExtractCompressedOrRaw(src, out var type, out var _);
            if (type == GetCompressorMethod())
            {
                var compressedStream = new MemoryStream(data);

                using var decompressorStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
                using (var decompressedStream = new MemoryStream())
                {
                    decompressorStream.CopyTo(decompressedStream);
                    return decompressedStream.ToArray();
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
