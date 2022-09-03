using System.Text;
using Interfaces;

namespace DataExchangeNET6.Processing
{
    internal class CallbackRegistrationSender : IDisposable
    {
        public static readonly string CALLBACK_REGISTRATION = "#CR#";

        private ICommonPipe? m_pipeConnection;
        private bool m_async;

        public CallbackRegistrationSender(ICommonPipe pipeConnection)
        {
            m_pipeConnection = pipeConnection;
            m_async = pipeConnection is IClientPipeAsync;
        }

        public void Dispose()
        {
        }

        public void Send(string callbackPipeName)
        {
            try
            {
                if (m_async)
                {
                    (m_pipeConnection as IClientPipeAsync)?.WriteBytes(Encoding.UTF8.GetBytes(CALLBACK_REGISTRATION + callbackPipeName), new CancellationTokenRegistration().Token);
                }
                else
                {
                    (m_pipeConnection as IClientPipe)?.WriteBytes(Encoding.UTF8.GetBytes(CALLBACK_REGISTRATION + callbackPipeName));
                }
            }
            catch (Exception)
            {
                // Log
            }
        }
    }
}
