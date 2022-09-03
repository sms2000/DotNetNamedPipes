using System.IO.Pipes;

namespace Interfaces
{
    public interface IServerManager : IDisposable
    {
        int UsedInstances { get; }
        string FullPipeName { get; }
        void PipeConnected(NamedPipeServerStream serverPipe);
        void PipeDisconnected(IPipeService pipeService, NamedPipeServerStream serverStream);
    }
}
