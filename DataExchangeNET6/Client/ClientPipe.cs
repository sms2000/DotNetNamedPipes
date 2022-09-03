#nullable enable
using System.IO.Pipes;
using DataExchangeNET6.Pipes;
using DataExchangeNET6.Transport;
using Interfaces;
using PipeSecurityHelper;

namespace DataExchangeNET6.Client
{
    public class ClientPipe : PipeCompression, IClientPipe
    {
        private const int BufferLimit = 128 * 1024 * 1024;

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
        public ClientPipe(string pipeName, string serverName = @".", int compressIfLongerThan = 128)
        {
            m_pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            m_serverName = serverName ?? throw new ArgumentNullException(nameof(serverName));
            m_compressIfLongerThan = compressIfLongerThan;

            // Log
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
            m_namedPipeClientStream?.Dispose();
            GC.SuppressFinalize(this);

            // Log
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
        /// Connect
        /// </summary>
        /// <param name="connectTimeoutMs"></param>
        public virtual bool Connect(long connectTimeoutMs = 0)
        {
            try
            {
                m_namedPipeClientStream = PipeSecurityProvider.CreateSecureClientStream(m_pipeName, m_serverName);

                if (connectTimeoutMs > 0) 
                {
                    m_namedPipeClientStream.Connect((int)connectTimeoutMs);
                }
                else 
                {
                    m_namedPipeClientStream.Connect();
                }
                
                // Log
                return true;
            }
            catch(TimeoutException ex)
            {
                // Log
                m_namedPipeClientStream?.Dispose();
                m_namedPipeClientStream = default;
                Console.WriteLine("Timeout: " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Log
                m_namedPipeClientStream?.Dispose();
                m_namedPipeClientStream = default;
                Console.WriteLine("Access violation: " + ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Close
        /// </summary>
        public void Close()
        {
            m_namedPipeClientStream?.Close();
            // Log
        }

        /// <summary>
        /// Write some bytes
        /// </summary>
        /// <param name="buffer"></param>
        public void WriteBytes(byte[] buffer)
        {
            WriteBytes(buffer, out var _);
        }

        /// <summary>
        /// Write some bytes and check the time spent
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="timeSpentInWriteMs"></param>
        public void WriteBytes(byte[] buffer, out long timeSpentInWriteMs)
        {
            if (m_namedPipeClientStream == null)
            {
                throw new ArgumentNullException(nameof(m_namedPipeClientStream));
            }

            DateTime now = DateTime.UtcNow;
            var transportHeader = new TransportHeader(buffer.Length);
            buffer = PreProcessDataForWrite(buffer, ref transportHeader, m_compressIfLongerThan);

            m_namedPipeClientStream.Write(transportHeader.Serialize());
            m_namedPipeClientStream.Write(buffer, 0, buffer.Length);

            timeSpentInWriteMs = (long)(DateTime.UtcNow - now).TotalMilliseconds;
        }

        /// <summary>
        /// Read some bytes
        /// </summary>
        /// <param name="buffer"></param>
        public byte[]? ReadBytes()
        {
            return ReadBytes(out var _);
        }

        /// <summary>
        /// Read some bytes
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="timeSpentInReadMs"></param>
        public byte[]? ReadBytes(out long timeSpentInReadMs)
        {
            if (m_namedPipeClientStream == default)
            {
                timeSpentInReadMs = 0;
                return default;
            }

            DateTime now = DateTime.UtcNow;

            try
            {
                var thArr = new byte[TransportHeader.HeaderLength];
                if (m_namedPipeClientStream.Read(thArr) == TransportHeader.HeaderLength)
                {
                    var transportHeader = TransportHeader.Reconstruct(thArr);
                    if (transportHeader.PayloadLength > 0)
                    {
                        if (transportHeader.PayloadLength > BufferLimit)
                        {
                            // Log
                        }
                        else
                        {
                            var buffer = new byte[transportHeader.CompressedLength];

                            if (m_namedPipeClientStream.Read(buffer, 0, transportHeader.CompressedLength) == transportHeader.CompressedLength)
                            {
                                buffer = PostProcessDataAfterRead(buffer, ref transportHeader);
                                return buffer;
                            }
                        }
                    }
                }
            }
            finally
            {
                timeSpentInReadMs = (long)(DateTime.UtcNow - now).TotalMilliseconds;
            }

            return default;
        }
    }
}
