namespace Interfaces
{
    public interface IDataExchangeProcessor
    {
        byte[]? DoExchange<T>(byte[] request, T processor, IConnection pipe) where T : class;
    }
}
