using Interfaces;
using DataExchangeNET6.Processing;

namespace DataExchangeNET6.Pipes
{
    internal class ServerPipeConnection : IConnection
    {
        private ISignalEventCallback? m_signalEventCallback;
        private ISignalProcessor? m_callbackRegistrationProcessor;
        private ICallbackProcessing? m_callbackProcessing;

        public ServerPipeConnection(ISignalEventCallback signalEventCallback, ICallbackProcessing callbackProcessing)
        {
            m_signalEventCallback = signalEventCallback;
            m_callbackProcessing = callbackProcessing;
            m_callbackRegistrationProcessor = new CallbackRegistrationProcessor(this);
        }

        public void ProcessServiceParcel(byte[] parcel)
        {
            if (m_callbackRegistrationProcessor != null
                &&
                m_callbackRegistrationProcessor.ProcessPotentialSignal(parcel))
            {
                m_callbackRegistrationProcessor = null; // One time only
            }

            // Log
        }

        public void SendCallbackPipeName(string callbackPipeName)
        {
            m_signalEventCallback?.Signal(this, new CallbackRegistrationEvent(callbackPipeName));
        }

        public ICallbackProcessing? GetCallbackProcessor()
        {
            return m_callbackProcessing;
        }
    }
}
