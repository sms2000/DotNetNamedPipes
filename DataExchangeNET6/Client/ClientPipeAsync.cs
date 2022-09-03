using System.IO.Pipes;
using Interfaces;
using PipeSecurityHelper;

namespace DataExchangeNET6.Client
{
    public class ClientPipeAsync<T> : PipeHelper<T>, IClientPipeAsync where T : class
    {
        private readonly string m_serverName;
        private readonly string m_pipeName;
        private NamedPipeClientStream? m_namedPipeClientStream;
        private readonly int m_compressIfLongerThan;

        /// <summary>
        /// Create synchronous client pipe
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="serverName"></param>
        /// <param name="compressIfLongerThan"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ClientPipeAsync(string pipeName, string serverName = @".", int compressIfLongerThan = 128)
        {
            m_pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            m_serverName = serverName ?? throw new ArgumentNullException(nameof(serverName));
            m_compressIfLongerThan = compressIfLongerThan;

            // Log
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            m_namedPipeClientStream?.Dispose();
            GC.SuppressFinalize(this);

            // Log
        }

        /// <summary>
        /// 
        /// </summary>
        public void Close()
        {
            m_namedPipeClientStream?.Close();
        }

        /// <summary>
        /// Returns the name of the pipe
        /// </summary>
        /// <returns></returns>
        public string GetPipeName()
        {
            return m_pipeName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectTimeoutMs"></param>
        /// <returns></returns>
        public bool Connect(long connectTimeoutMs = 0)
        {
            return Connect(new CancellationTokenRegistration().Token, connectTimeoutMs);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="connectTimeoutMs"></param>
        /// <returns></returns>
        public bool Connect(CancellationToken token, long connectTimeoutMs = 0)
        {
            try
            {
                m_namedPipeClientStream = PipeSecurityProvider.CreateSecureClientStreamAsync(m_pipeName, m_serverName);

                var tasks = new Task[2];

                tasks[0] = connectTimeoutMs > 0 ? m_namedPipeClientStream.ConnectAsync((int)connectTimeoutMs, token) : m_namedPipeClientStream.ConnectAsync(token);
                tasks[1] = tasks[0].ContinueWith(task => finishedConnected(task, tasks), token);

                try
                {
                    Task.WhenAll(tasks).Wait(token);
                    // Log
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }

                    // Log
                    return false;
                }

                // Log
                return true;
            }
            catch (TimeoutException)
            {
                // Log
                m_namedPipeClientStream?.Dispose();
                m_namedPipeClientStream = default;
            }
            catch (UnauthorizedAccessException)
            {
                // Log
                m_namedPipeClientStream?.Dispose();
                m_namedPipeClientStream = default;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public byte[]? ReadBytes(CancellationToken token)
        {
            return ReadBytes(token, out _);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="timeSpentInReadMs"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public byte[]? ReadBytes(CancellationToken token, out long timeSpentInReadMs)
        {
            if (m_namedPipeClientStream == null)
            {
                throw new ArgumentNullException(nameof(m_namedPipeClientStream));
            }

            DateTime now = DateTime.UtcNow;
            var result = ReadAsyncStep(m_namedPipeClientStream, token);
            timeSpentInReadMs = (long)(DateTime.UtcNow - now).TotalMilliseconds;

            // Log
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="token"></param>
        public bool WriteBytes(byte[] buffer, CancellationToken token)
        {
            return WriteBytes(buffer, token, out _);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="token"></param>
        /// <param name="timeSpentInWriteMs"></param>
        public bool WriteBytes(byte[] buffer, CancellationToken token, out long timeSpentInWriteMs)
        {
            if (m_namedPipeClientStream == null)
            {
                throw new ArgumentNullException(nameof(m_namedPipeClientStream));
            }

            DateTime now = DateTime.UtcNow;
            var result = WriteAsyncStep(m_namedPipeClientStream, buffer, token);
            timeSpentInWriteMs = (long)(DateTime.UtcNow - now).TotalMilliseconds;

            // Log
            return result;
        }

        protected override int GetMinimalLengthToCompress()
        {
            return m_compressIfLongerThan;
        }

        #region private
        private void finishedConnected(Task task, Task[] tasks)
        {
            // Logs
        }

        #endregion
    }
}
