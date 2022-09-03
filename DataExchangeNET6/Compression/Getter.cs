using Interfaces;

namespace Compression
{
    public static class Getter
    {
        private static readonly ICompressor m_deflator = new Deflator();
        private static readonly ICompressor m_brotli = new Brotli();
        private static readonly ICompressor m_brotliStrong = new BrotliStrong();
        private static readonly ICompressor m_gZipFast = new GZipFast();

        public static ICompressor? GetRecommendedCompressor()
        {
            return m_gZipFast;
        }

        public static ICompressor? GetCompressorByType(ICompressor.Compressor compressorType)
        {
            return compressorType switch
            {
                ICompressor.Compressor.Brotli => m_brotli,
                ICompressor.Compressor.BrotliStrong => m_brotliStrong,
                ICompressor.Compressor.Deflator => m_deflator,
                ICompressor.Compressor.GZipFast => m_gZipFast,
                _ => null,
            };
        }
    }
}
