namespace Interfaces
{
    public interface ICompressor
    {
        enum Compressor
        {
            None, 
            Deflator,
            Brotli,
            BrotliStrong,
            GZipFast
        }

        Compressor GetCompressorMethod();

        byte[] Deflate(byte[] src);
        byte[] Inflate(byte[] src);
    }
}
