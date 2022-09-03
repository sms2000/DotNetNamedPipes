using Interfaces;
using System.IO.Compression;

namespace Compression
{
    public class GZipFast : CompressorHelper, ICompressor
    {
        public ICompressor.Compressor GetCompressorMethod()
        {
            return ICompressor.Compressor.GZipFast;
        }

        public byte[] Deflate(byte[] src)
        {
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionLevel.Fastest))
                {
                    gzip.Write(src, 0, src.Length);
                }

                var memory = ms.ToArray();
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
            var data = ExtractCompressedOrRaw(src, out var type, out var uncompressedSize);
            if (type == GetCompressorMethod())
            {
                using var memoryStream = new MemoryStream(data);
                using (var gzip = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    var buffer = new byte[uncompressedSize];
                    gzip.Read(buffer, 0, uncompressedSize);
                    return buffer;
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
