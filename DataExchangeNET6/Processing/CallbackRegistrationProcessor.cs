using System.Text;
using Interfaces;

namespace DataExchangeNET6.Processing
{
    internal class CallbackRegistrationProcessor : ISignalProcessor
    {
        private readonly IConnection m_connection;

        public CallbackRegistrationProcessor(IConnection connection)
        {
            m_connection = connection;
        }

        public bool ProcessPotentialSignal(byte[] request)
        {
            var decoded = Encoding.UTF8.GetString(request);

            if (decoded.StartsWith(CallbackRegistrationSender.CALLBACK_REGISTRATION))
            {
                var callbackPipeName = decoded[CallbackRegistrationSender.CALLBACK_REGISTRATION.Length..];
                m_connection.SendCallbackPipeName(callbackPipeName);
                return true;
            }

            return false;
        }
    }
}
