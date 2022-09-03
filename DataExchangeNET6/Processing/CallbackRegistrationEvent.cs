using Interfaces;

namespace DataExchangeNET6.Processing
{
    internal class CallbackRegistrationEvent : ISignalEvent
    {
        public string CallbackPipeName { get; private set; }

        public CallbackRegistrationEvent(string callbackPipeName)
        {
            CallbackPipeName = callbackPipeName;
        }
    }
}
