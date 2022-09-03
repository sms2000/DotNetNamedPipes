using Interfaces;

namespace DataExchangeNET6.Transport
{
    public class TransportHeader
    {
        private int m_payloadLength;
        private int m_compressedLength;
        private ICompressor.Compressor m_compressionMethod;

        public TransportHeader(int payloadLength)
        {
            m_payloadLength = payloadLength;
            m_compressedLength = payloadLength;
            m_compressionMethod = ICompressor.Compressor.None;
        }

        public TransportHeader(int payloadLength, int compressedLength, ICompressor.Compressor method)
        {
            m_payloadLength = payloadLength;
            m_compressedLength = compressedLength;
            m_compressionMethod = method;
        }

        public static int HeaderLength => sizeof(int) + sizeof(int) + sizeof(int);
        public int PayloadLength => m_payloadLength;
        public int CompressedLength => m_compressedLength;
        public ICompressor.Compressor CompressionMethod => m_compressionMethod;

        public byte[] Serialize()
        {
            var buffer = new byte[HeaderLength];
            Array.Copy(BitConverter.GetBytes(m_payloadLength), 0, buffer, 0, sizeof(int));
            Array.Copy(BitConverter.GetBytes(m_compressedLength), 0, buffer, sizeof(int), sizeof(int));
            Array.Copy(BitConverter.GetBytes((int)m_compressionMethod), 0, buffer, sizeof(int) + sizeof(int), sizeof(int));

            return buffer;
        }

        public static TransportHeader Reconstruct(byte[] arrBytes)
        {
            var pl = BitConverter.ToInt32(arrBytes, 0);
            var cl = BitConverter.ToInt32(arrBytes, sizeof(int));
            var cm = BitConverter.ToInt32(arrBytes, sizeof(int) + sizeof(int));
            var method = (ICompressor.Compressor)Enum.Parse(typeof(ICompressor.Compressor), cm.ToString());

            return new TransportHeader(pl, cl, method);
        }

        public void UpdateCompressionData(ICompressor.Compressor compressorMethod, int compressedLength)
        {
            m_compressionMethod = compressorMethod;
            m_compressedLength = compressedLength;
        }
    }
}
