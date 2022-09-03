using System.IO.Pipes;
using Interfaces;
using PipeSecurityHelper;

namespace DataExchangeNET6
{
    internal class ListenerThread<T> : IDisposable where T : class
    {
        private readonly IServerManager m_serverManager;
        private readonly Thread m_thread;
        private bool m_disposed;
        private bool m_newClientConnected;
        private readonly string m_constructedPipeName;
        private readonly int m_instances;
        private readonly long m_pipeConnectTimeoutMS;
        private readonly PipeSecurityProvider m_pipeProvider;
        private HashSet<NamedPipeServerStream> m_listeningPipes = new ();

        /// <summary>
        /// Creates the main listener
        /// </summary>
        /// <param name="serverManager"></param>
        /// <param name="constructedPipeName"></param>
        /// <param name="pipeProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ListenerThread(IServerManager serverManager, string constructedPipeName, PipeSecurityProvider pipeProvider) : 
                                this(serverManager, constructedPipeName, pipeProvider, 1, 0)
        {
        }

        /// <summary>
        /// Creates the main listener
        /// </summary>
        /// <param name="serverManager"></param>
        /// <param name="constructedPipeName"></param>
        /// <param name="pipeProvider"></param>
        /// <param name="maxInstances"></param>
        /// <param name="pipeConnectTimeoutMS"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ListenerThread(IServerManager serverManager, string constructedPipeName, PipeSecurityProvider pipeProvider, int maxInstances, long pipeConnectTimeoutMS)
        {
            m_serverManager = serverManager ?? throw new ArgumentNullException(nameof(serverManager));
            m_constructedPipeName = constructedPipeName ?? throw new ArgumentNullException(nameof(constructedPipeName));
            m_pipeProvider = pipeProvider ?? throw new ArgumentNullException(nameof(pipeProvider));

            m_pipeConnectTimeoutMS = pipeConnectTimeoutMS >= 0 ? pipeConnectTimeoutMS : throw new ArgumentOutOfRangeException(nameof(pipeConnectTimeoutMS));
            m_instances = maxInstances;

            m_thread = new Thread(runner);
        }

        /// <summary>
        /// Starts the listener
        /// </summary>
        public void Start()
        {
            m_thread.Start();
        }

        /// <summary>
        /// Finishes the listener
        /// </summary>
        public void Dispose()
        {
            m_disposed = true;

            foreach (var listeningPipe in m_listeningPipes)
            {
                listeningPipe.Close();
                listeningPipe.Dispose();

#if DEBUG
                Console.WriteLine("Listening pipe [" + m_constructedPipeName + "] disposed off");
#endif
            }

            m_listeningPipes.Clear();
        }

        #region private
        private void runner()
        {
            bool allUsed = false;

            while(!m_disposed)
            {
                if (m_serverManager.UsedInstances >= m_instances)
                {
                    if (!allUsed)
                    {
                        allUsed = true;
                        // Log

#if DEBUG
                        Console.WriteLine("No free instances found for [" + m_constructedPipeName + "]. Waiting...");
#endif
                    }

                    Thread.Sleep(1000);
                    continue;
                }

#if DEBUG
                Console.WriteLine("Back to listening for [" + m_constructedPipeName + "].");
#endif

                allUsed = false;
                var attemptedToConnect = DateTime.UtcNow;
                
                listen();

                while (!m_disposed)
                {
                    if (m_newClientConnected)
                    {
                        // Log -> Connected!
                        m_newClientConnected = false;
                        break;
                    }
                    
                    if (m_pipeConnectTimeoutMS > 0 && (DateTime.UtcNow - attemptedToConnect).TotalMilliseconds > m_pipeConnectTimeoutMS)
                    {
                        // Timeout?
                        // TBD what to do if we're waiting too long.
                    }

                    Thread.Sleep(1000);
                    // Trace log
                }
            }
        }

        private void listen()
        {
            m_newClientConnected = false;

            try
            {
#if DEBUG
                Console.WriteLine("Starting to listen...");
#endif
                var listeningPipe = m_pipeProvider.CreateSecureServerStreamAsync(m_constructedPipeName, m_instances);
                m_listeningPipes.Add(listeningPipe);

                // Wait for a connection
                listeningPipe.BeginWaitForConnection (waitForConnectionCallBack, listeningPipe);
            }
#if DEBUG
            catch (Exception ex)
#else
            catch (Exception)
#endif
            {
                // Log
#if DEBUG
                Console.WriteLine("Exception: " + ex.Message);
#endif
            }
        }

        private void waitForConnectionCallBack(IAsyncResult asyncResult)
        {
            if (asyncResult.AsyncState == null)
            {
                // Log
                return;
            }

            try
            {
                if (asyncResult.AsyncState is NamedPipeServerStream listeningPipe)
                {
                    m_listeningPipes.Remove(listeningPipe);

                    listeningPipe.EndWaitForConnection(asyncResult);
                    m_newClientConnected = true;
                    // Log

                    // Inform the ServerManager about the new pipe
                    m_serverManager.PipeConnected(listeningPipe);

#if DEBUG
                    Console.WriteLine("Server pipe {0} connected and used for IPC", listeningPipe.GetHashCode());
#endif
                }

            }
#if DEBUG
            catch (Exception ex)
#else
            catch (Exception)
#endif
            {
                // Log

#if DEBUG
                Console.WriteLine("Exception: " + ex.Message);
#endif
            }
        }
        #endregion
    }
}
