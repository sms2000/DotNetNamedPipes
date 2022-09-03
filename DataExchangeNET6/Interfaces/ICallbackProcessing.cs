namespace Interfaces
{
    public interface ICallbackProcessing
    {
        void RegisterCallbackProperties<T>(string callbackPipeName, T callbackProcessor);
        T? CreateCallbackChannel<T>() where T : class;
        string CallbackChannelPipeName { get; }
        string? CallbackTypeFullName { get; }
    }
}
