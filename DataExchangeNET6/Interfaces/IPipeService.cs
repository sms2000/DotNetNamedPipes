using System.IO.Pipes;

namespace Interfaces
{
    public interface IPipeService : IDisposable, ICallbackProcessing
    {
        NamedPipeServerStream ServerStream { get; }
        IConnection ServerConnection { get; }
        string ServiceTypeFullName { get; }
    }
}
