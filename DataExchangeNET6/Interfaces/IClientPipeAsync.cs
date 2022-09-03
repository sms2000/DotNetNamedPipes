namespace Interfaces
{
    public interface IClientPipeAsync : ICommonPipe
    {
        bool WriteBytes(byte[] buffer, CancellationToken token);
        bool WriteBytes(byte[] buffer, CancellationToken token, out long timeSpentInWriteMs);
        byte[]? ReadBytes(CancellationToken token);
        byte[]? ReadBytes(CancellationToken token, out long timeSpentInReadMs);
    }
}
