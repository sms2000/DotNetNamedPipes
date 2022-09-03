namespace Interfaces
{
    public interface IConnection
    {
        void ProcessServiceParcel(byte[] parcel);
        void SendCallbackPipeName(string callbackPipeName);
        ICallbackProcessing? GetCallbackProcessor();
    }
}
