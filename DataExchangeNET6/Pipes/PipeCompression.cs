using DataExchangeNET6.Transport;

namespace DataExchangeNET6.Pipes
{
    public class PipeCompression
    {
        private const int MinimumLengthToCompress = 128;
        private const int LargeBufferBytes = 8192;

        /// <summary>
        /// Cpmpress data wisely if required and
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="transportHeader"></param>
        /// <param name="minimalLengthToCompress"></param>
        /// <returns></returns>
        protected static byte[] PreProcessDataForWrite(byte[] buffer, ref TransportHeader transportHeader, int minimalLengthToCompress = MinimumLengthToCompress, int minimalLengthForExtraStrongCompression = LargeBufferBytes)
        {
            if (buffer.Length >= minimalLengthToCompress)
            {
                var compressor = buffer.Length >= minimalLengthForExtraStrongCompression ? Compression.Getter.GetCompressorByType(Interfaces.ICompressor.Compressor.BrotliStrong) : 
                                                                                           Compression.Getter.GetRecommendedCompressor();
                if (compressor != null)
                {
                    var compressed = compressor.Deflate(buffer);
                    if (compressed.Length < buffer.Length)
                    {
                        transportHeader.UpdateCompressionData(compressor.GetCompressorMethod(), compressed.Length);
                        return compressed;
                    }
                    else
                    {
                        // Log - cannot compress
                    }
                }
            }

            return buffer;
        }

        /// <summary>
        /// Compress data with a given compressor
        /// </summary>
        /// <param name="compressor"></param>
        /// <param name="buffer"></param>
        /// <param name="transportHeader"></param>
        /// <param name="minimalLengthToCompress"></param>
        /// <returns></returns>
        protected static byte[] PreProcessDataForWrite(Interfaces.ICompressor compressor, byte[] buffer, ref TransportHeader transportHeader, int minimalLengthToCompress = MinimumLengthToCompress)
        {
            if (buffer.Length >= minimalLengthToCompress)
            {
                var compressed = compressor.Deflate(buffer);
                transportHeader.UpdateCompressionData(compressor.GetCompressorMethod(), compressed.Length);
                return compressed;
            }

            return buffer;
        }

        /// <summary>
        /// Decompress data if needed
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="transportHeader"></param>
        /// <returns></returns>
        /// <exception cref="DecompressException"></exception>
        protected static byte[] PostProcessDataAfterRead(byte[] buffer, ref TransportHeader transportHeader)
        {
            if (buffer == null || buffer.Length == 0)
            {
                return Array.Empty<byte>();
            }

            var method = Compression.Getter.GetCompressorByType(transportHeader.CompressionMethod);
            if (method != null)
            {
                var result = method.Inflate(buffer);
                if (result.Length != transportHeader.PayloadLength)
                {
                    throw new DecompressException("Failed to decompress");
                }

                return result;
            }

            return buffer;
        }
    }
}
