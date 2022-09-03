using System.Collections.Concurrent;
using System.IO.Pipes;
using Interfaces;
using DataExchangeNET6.Processing;
using DataExchangeNET6.Server;
using PipeSecurityHelper;

namespace DataExchangeNET6
{
    public class AsyncServerManager<T> : IServerManager, ISignalEventCallback where T : class
    {
        private const int MaxConcurentPipeInstances = 254; // This is by .NET

        private long m_pipeIndex = 0;
        private readonly ConcurrentDictionary<long, IPipeService> m_pipeServices = new();
        private readonly string m_serverName;
        private readonly string m_pipeName;
        private readonly string m_fullPipeName;
        private readonly ListenerThread<T> m_thread;
        private readonly T m_requestsProcessor;
        private readonly IDataExchangeProcessorAsync m_dataExchangeProcessor;
        private readonly int m_compressIfLongerThan;

        public long MaxServerPipeInstances { get; set; } = 100;
        public long PipeConnectTimeoutMS { get; set; } = 0;
        public long PipeExchangeTimeoutMS { get; set; } = 0;
        public long PipeReadTimeoutMS { get; set; } = 0;
        public int UsedInstances => m_pipeServices.Count;
        public string FullPipeName => m_fullPipeName;

        /// <summary>
        /// Builds the async pipe server manager
        /// </summary>
        /// <param name="requestProcessor"></param>
        /// <param name="pipeName"></param>
        /// <param name="pipeProvider"></param>
        /// <param name="dataExchangeProcessor"></param>
        /// <param name="serverName"></param>
        /// <param name="compressIfLongerThan"></param>
        /// <param name="maxInstances"></param>
        /// <param name="pipeConnectionTimeoutMs"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AsyncServerManager(T requestProcessor, string pipeName, PipeSecurityProvider pipeProvider, IDataExchangeProcessorAsync dataExchangeProcessor, 
                                    string serverName = @".", int compressIfLongerThan = 128, int maxInstances = MaxConcurentPipeInstances, long pipeConnectionTimeoutMs = 0)
        {
            m_pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            m_serverName = serverName ?? throw new ArgumentNullException(nameof(serverName));
            m_dataExchangeProcessor = dataExchangeProcessor;
            m_requestsProcessor = requestProcessor ?? throw new ArgumentNullException(nameof(requestProcessor));
            m_compressIfLongerThan = compressIfLongerThan;

            if (OperatingSystem.IsWindows()) 
            {
                m_fullPipeName = $"\\\\{m_serverName}\\{m_pipeName}";

                // Log
            }
            else
            {
                m_fullPipeName = $"{m_pipeName}";

                // Log
            }

            m_thread = new ListenerThread<T>(this, m_fullPipeName, pipeProvider, maxInstances, pipeConnectionTimeoutMs);
        }

        /// <summary>
        /// Starts the Manager
        /// </summary>
        public void Start()
        {
            // Start the async listener thread
            m_thread.Start();
        }

        /// <summary>
        /// Implements Dispose
        /// </summary>
        public void Dispose()
        {
            foreach(var serverPipe in m_pipeServices.Values)
            {
                serverPipe.Dispose();

#if DEBUG
                Console.WriteLine("Pipe disposed of: " + serverPipe.ServerStream.GetHashCode());
#endif
            }

            m_thread?.Dispose();

            GC.SuppressFinalize(this);
        }

        #region IServerManager
        /// <summary>
        /// Callback: pipe connected
        /// </summary>
        /// <param name="serverPipe"></param>
        public void PipeConnected(NamedPipeServerStream serverPipe)
        {
            var pipeService = new PipeService<T>(this, m_requestsProcessor, serverPipe, m_dataExchangeProcessor, this, PipeReadTimeoutMS, compressIfLongerThan: m_compressIfLongerThan);
            m_pipeServices[m_pipeIndex++] = pipeService;

            // Log
#if DEBUG
            Console.WriteLine("New pipe connected: " + serverPipe.GetHashCode());
#endif
        }

        /// <summary>
        /// Callback: pipe disconnected
        /// </summary>
        /// <param name="pipeService"></param>
        /// <param name="serverPipe"></param>
        public void PipeDisconnected(IPipeService pipeService, NamedPipeServerStream serverPipe)
        {
            var removed = false;

            foreach (var key in m_pipeServices.Keys)
            {
                if (pipeService.Equals(m_pipeServices[key]))
                {
                    removed = true;
                    m_pipeServices.Remove(key, out _);
                    // Log
                    break;
                }
            }

            if (!removed)
            {
                // Log
            }

            pipeService.Dispose();

#if DEBUG
            Console.WriteLine("Pipe disposed of: " + serverPipe.GetHashCode());
#endif

            // Log
        }
        #endregion

        #region ISignalEventCallback
        /// <summary>
        /// Signal callback
        /// </summary>
        /// <param name="pipe"></param>
        /// <param name="signalEvent"></param>
        public void Signal(IConnection pipe, ISignalEvent signalEvent)
        {
            if (signalEvent is not CallbackRegistrationEvent callbackRegistrationEvent)
            {
                // Log
                return;
            }

            foreach (var lookUp in m_pipeServices.Values)
            {
                if (lookUp.ServerConnection.Equals(pipe))
                {
                    lookUp.RegisterCallbackProperties(callbackRegistrationEvent.CallbackPipeName, m_requestsProcessor);
                    // Log - callback registered
                    return;
                }
            }

            // Log - callback registration failed
        }
        #endregion
    }
}
