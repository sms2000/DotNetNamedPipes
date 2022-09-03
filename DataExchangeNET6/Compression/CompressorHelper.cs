using static Interfaces.ICompressor;

namespace Compression
{
    public class CompressorHelper
    {
        private const int TypeBytes = sizeof(byte);
        private const int SizeBytes = sizeof(int);

        protected static byte[] FillHeaderCompressed(byte[] compressed, int compressedLength, int uncompressedLength, Compressor method)
        {
            var output = new byte[compressedLength + TypeBytes + SizeBytes];
            Array.Copy(new byte[] { (byte)method }, 0, output, 0, TypeBytes);
            Array.Copy(BitConverter.GetBytes(uncompressedLength), 0, output, TypeBytes, SizeBytes);
            Array.Copy(compressed, 0, output, TypeBytes + SizeBytes, compressedLength);
            return output;
        }

        protected static byte[] FillHeaderRaw(byte[] raw, long uncompressedLength)
        {
            var output = new byte[raw.Length + TypeBytes];
            Array.Copy(new byte[] { (byte)Compressor.None }, 0, output, 0, TypeBytes);
            Array.Copy(BitConverter.GetBytes(uncompressedLength), 0, output, TypeBytes, SizeBytes);
            return output;
        }

        protected static byte[] ExtractCompressedOrRaw(byte[] src, out Compressor type, out int uncompressedSize)
        {
            type = (Compressor)Enum.Parse(typeof(Compressor), src[0].ToString());
            if (type == Compressor.None)
            {
                uncompressedSize = src.Length - TypeBytes;
                return src.Skip(TypeBytes).ToArray();
            }

            uncompressedSize = BitConverter.ToInt32(src, TypeBytes);
            return src.Skip(TypeBytes + SizeBytes).ToArray();
        }
    }
}
