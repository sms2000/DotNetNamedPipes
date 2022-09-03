namespace Interfaces
{
    public interface ISignalEventCallback
    {
        void Signal(IConnection pipe, ISignalEvent signalEvent);
    }
}
