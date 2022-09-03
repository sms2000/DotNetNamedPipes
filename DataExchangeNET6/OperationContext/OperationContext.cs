using System.Collections.Concurrent;
using Interfaces;

namespace DataExchangeNET6
{
    public static class OperationContext
    {
        private static readonly ConcurrentDictionary<int, CurrentContext> m_contextMap = new();

        static OperationContext()
        {
        }

        /// <summary>
        /// Store the current context
        /// </summary>
        /// <param name="connection"></param>
        /// <exception cref="NullReferenceException"></exception>
        public static void PopulateCurrentContext(ICallbackProcessing connection)
        {
            var tid = Task.CurrentId;
            if (tid == null)
            {
                throw new NullReferenceException("Current TaskID = 'null'");
            }

            if (m_contextMap.TryGetValue(tid.Value, out var context))
            {
                context.CloseContext();
                // Log
            }

            m_contextMap[tid.Value] = new CurrentContext(connection);
        }

        /// <summary>
        /// Dismisses the current context
        /// </summary>
        /// <param name="connection"></param>
        /// <exception cref="NullReferenceException"></exception>
        public static void FreeCurrentContent(ICallbackProcessing connection)
        {
            var tid = Task.CurrentId;
            if (tid == null)
            {
                throw new NullReferenceException("Current TaskID = 'null'");
            }

            if (!m_contextMap.TryGetValue(tid.Value, out var context))
            {
                // Log
                return;
            }

            context.CloseContext();

            m_contextMap.TryRemove(tid.Value, out _);
            // Log
        }

        /// <summary>
        /// Return the current context
        /// </summary>
        public static CurrentContext Current
        {
            get
            {
                var tid = Task.CurrentId;
                if (tid == null)
                {
                    throw new NullReferenceException("Current TaskID = 'null'");
                }

                if (!m_contextMap.ContainsKey(tid.Value))
                {
                    throw new AccessViolationException("No context for the TID=" + tid.Value);
                }

                return m_contextMap[tid.Value];
            }
        }

        public class CurrentContext
        {
            private ICallbackProcessing? m_serverConnection;
            private object? m_callbackConnection;

            internal CurrentContext(ICallbackProcessing serverConnection)
            {
                m_serverConnection = serverConnection;
            }

            public T GetCallbackChannel<T>() where T : class
            {
                if (m_serverConnection == null)
                {
                    throw new InvalidOperationException("Invalid server-side connection");
                }

                if (m_callbackConnection != null)
                {
                    return (T)m_callbackConnection;
                }

                var callback = m_serverConnection.CreateCallbackChannel<T>();
                if (callback != null)
                {
                    m_callbackConnection = callback;
                    return callback;
                }

                throw new InvalidOperationException("Failed to create callback channel");
            }

            internal void CloseContext()
            {
            }
        }
    }
}
