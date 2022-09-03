namespace Interfaces
{
    public interface IClientPipe : ICommonPipe
    {
        void WriteBytes(byte[] buffer);
        void WriteBytes(byte[] buffer, out long timeSpentInWriteMs);
        byte[]? ReadBytes();
        byte[]? ReadBytes(out long timeSpentInReadMs);
    }
}
