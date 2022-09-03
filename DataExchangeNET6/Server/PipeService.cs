#nullable enable
using System.IO.Pipes;
using System.ServiceModel;
using Interfaces;
using DataExchangeNET6.Exchange;
using DataExchangeNET6.Pipes;

namespace DataExchangeNET6.Server
{
    internal class PipeService<T> : PipeHelper<T>, IPipeService where T : class
    {
        private const byte ServiceParcelFirstByte = (byte)'#';
        
        private readonly NamedPipeServerStream m_serverStream;
        private readonly IConnection m_serverConnection;
        private readonly CancellationTokenSource m_cancellationTokenSource;
        private readonly IDataExchangeProcessorAsync m_dataExchangeProcessor;
        private string m_callbackChannelPipeName = string.Empty;
        private object? m_callbackProcessor;
        private readonly T m_processor;
        private readonly IServerManager m_serverManager;
        private readonly Thread m_thread;
        private readonly long m_readTimeoutMs;
        private readonly int m_compressIfLongerThan;
        private bool m_disposed;
        private readonly string m_serviceTypeFullName;
        private readonly string? m_callbackTypeFullName;
        private object? m_callbackHost;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverManager"></param>
        /// <param name="processor"></param>
        /// <param name="serverStream"></param>
        /// <param name="dataExchangeProcessor"></param>
        /// <param name="signalEventCallback"></param>
        /// <param name="readTimeoutMs"></param>
        /// <param name="compressIfLongerThan"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PipeService(IServerManager serverManager, T processor, NamedPipeServerStream serverStream, IDataExchangeProcessorAsync dataExchangeProcessor,
                           ISignalEventCallback signalEventCallback, long readTimeoutMs = 0, int compressIfLongerThan = 128)
        {
            m_serviceTypeFullName = typeof(T).FullName ?? string.Empty;

            foreach (var attr in typeof(T).GetCustomAttributes(false))
            {
                if (attr is ServiceContractAttribute serviceContractAttribute)
                {
                    m_callbackTypeFullName = serviceContractAttribute.CallbackContract != default ? serviceContractAttribute.CallbackContract.FullName : default;
                    break;
                }
            }
            
            m_serverManager = serverManager ?? throw new ArgumentNullException(nameof(serverManager));
            m_processor = processor ?? throw new ArgumentNullException(nameof(processor));
            m_serverStream = serverStream ?? throw new ArgumentNullException(nameof(serverStream));
            m_dataExchangeProcessor = dataExchangeProcessor ?? throw new ArgumentNullException(nameof(dataExchangeProcessor));
            m_readTimeoutMs = readTimeoutMs;
            m_compressIfLongerThan = compressIfLongerThan;
            m_cancellationTokenSource = m_readTimeoutMs > 0 ? new CancellationTokenSource((int)m_readTimeoutMs) : new CancellationTokenSource();
            m_serverConnection = new ServerPipeConnection(signalEventCallback, this);
            m_thread = new Thread(runner);
            // Log

            m_thread.Start();
            // Log

#if DEBUG
            Console.WriteLine("Server pipe created: " + GetHashCode());
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            m_disposed = true;
            // Log 

            m_cancellationTokenSource.Cancel();
            // Log

            if (m_callbackHost != null)
            {
                IPCHelper.Close(m_callbackHost);
            }

            try
            {
                if (m_serverStream.IsConnected)
                {
                    m_serverStream.Disconnect();
                }

                m_serverStream.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public NamedPipeServerStream ServerStream => m_serverStream;
        public IConnection ServerConnection => m_serverConnection;
        public string ServiceTypeFullName => m_serviceTypeFullName;
        public string? CallbackTypeFullName => m_callbackTypeFullName;
        public string CallbackChannelPipeName => m_callbackChannelPipeName;

        protected byte[]? ProcessDataAsyncStep(byte[] buffer, CancellationToken token)
        {           
            var responseWrapper = new List<byte[]>();
            
            var processingTasks = new Task[2];
            processingTasks[0] = m_dataExchangeProcessor.DoExchangeAsync(buffer, responseWrapper, m_processor, m_serverConnection);
            processingTasks[1] = processingTasks[0].ContinueWith(task => FinishedRead(task, processingTasks, buffer), token);

            try
            {
#if DEBUG
                Console.WriteLine("Read from {0}, token {1}, tasks {2}", GetHashCode(), token.GetHashCode(), processingTasks.GetHashCode());
#endif

                Task.WhenAll(processingTasks).Wait(token);
                // Log
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    // Log
#if DEBUG
                    Console.WriteLine("Exception: " + ex.InnerException.Message + Environment.NewLine + "Stack: " + ex.StackTrace);
#endif

                    throw ex.InnerException;
                }
                else
                {
                    // Log
#if DEBUG
                    Console.WriteLine("Exception: " + ex.Message + Environment.NewLine + "Stack: " + ex.StackTrace);
#endif
                    throw;
                }
            }

            if (responseWrapper.Count != 1 || responseWrapper[0].Length == 0)
            {
                // Log
                return default;
            }

            return responseWrapper[0];
        }

        protected override int GetMinimalLengthToCompress()
        {
            return m_compressIfLongerThan;
        }

#region  ICallbackProcessing
        public void RegisterCallbackProperties<T1>(string callbackPipeName, T1 callbackProcessor)
        {
            m_callbackChannelPipeName = callbackPipeName;
            m_callbackProcessor = callbackProcessor;
        }

        public V? CreateCallbackChannel<V>() where V : class
        {
            if (m_callbackProcessor != null)
            {
                m_callbackHost ??= IPCHelper.CreateDuplexChannel<V>(m_callbackChannelPipeName, m_callbackProcessor);
                return m_callbackHost as V;
            }

            // Log
            return default;
        }
#endregion

#region private
        private void runner()
        {
            while (!m_disposed)
            {
                try
                {
                    // 1. Receive data
                    var buffer = ReadAsyncStep(m_serverStream, m_cancellationTokenSource.Token);
                    if (buffer == null)
                    {
                        m_serverManager.PipeDisconnected(this, m_serverStream);

                        // Log
#if DEBUG
                        Console.WriteLine("Pipe [{0}] disconnected", m_serverManager.FullPipeName);
#endif
                        break;
                    }

                    ReceivedData(buffer);

                    if (buffer[0] == ServiceParcelFirstByte)
                    {
                        // Log
#if DEBUG
                        Console.WriteLine("Pipe [{0}] processes a service parcel", m_serverManager.FullPipeName);
#endif

                        m_serverConnection.ProcessServiceParcel(buffer);
                        continue;
                    }

                    // 2. Process data
                    var response = ProcessDataAsyncStep(buffer, m_cancellationTokenSource.Token);

                    // 3. Respond
                    if (response is {Length: > 0})
                    {
                        var result = WriteAsyncStep(m_serverStream, response, m_cancellationTokenSource.Token);
                        // Log
                    }
                    else
                    {
#if DEBUG
                        Console.WriteLine("Parcel not processed for [{0}]. 'ProcessDataAsyncStep' returned 'null' or an empty buffer", 
                                          m_serverManager.FullPipeName);
#endif
                        // Log
                    }
                }
                catch (OperationCanceledException)
                {
                    m_disposed = true;
                    break;
                }
            }
        }
#endregion
    }
}
