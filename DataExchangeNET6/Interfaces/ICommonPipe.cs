namespace Interfaces
{
    public interface ICommonPipe : IDisposable
    {
        string GetPipeName();
        bool Connect(long connectTimeoutMs = 0);
        void Close();
    }
}
