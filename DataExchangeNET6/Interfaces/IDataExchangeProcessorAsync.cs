namespace Interfaces
{
    public interface IDataExchangeProcessorAsync
    {
        Task DoExchangeAsync<T>(byte[] request, List<byte[]> response, T processor, IConnection pipe, CancellationToken? token = null) where T : class;
    }
}
