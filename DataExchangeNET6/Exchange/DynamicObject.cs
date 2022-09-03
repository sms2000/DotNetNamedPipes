#nullable enable
using System.Dynamic;
using System.Text;
using Interfaces;

namespace DataExchangeNET6.Exchange
{
    /// <summary>
    /// Represents the dynamic object to serve as a proxy for Client-Server IPC
    /// </summary>
    [Serializable]
    public class DynamicObject : System.Dynamic.DynamicObject, IDynamicObject
    {
        private ICommonPipe? m_clientPipeConnection;
        private readonly bool m_async;

        public DynamicObject(ICommonPipe? clientPipeConnection, long connectTimeoutMs = 0)
        {
            m_clientPipeConnection = clientPipeConnection;
            m_async = clientPipeConnection is IClientPipeAsync;

            if (!Initialize(connectTimeoutMs))
            {
                throw new InvalidOperationException("Failed to connect");
            }
        }

        protected bool Initialize(long connectTimeoutMs)
        {
            if (m_clientPipeConnection != null)
            {
                try
                {
                    if (m_clientPipeConnection.Connect(connectTimeoutMs))
                    {
                        // Log
                        return true;
                    }
                }
                catch (Exception /* ex */)
                {
                    // Log
                }
            }

            // Log
            return false;
        }

        /// <summary>
        /// Dispose of the object
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            // Kill pipe (mind the call order)
            m_clientPipeConnection?.Close();
            m_clientPipeConnection?.Dispose();
            m_clientPipeConnection = null;
        }

        /// <summary>
        /// Two-way synchronous exchange
        /// </summary>
        /// <param name="encoded">Binary blob</param>
        /// <returns>Optional returned object</returns>
        public object? SendAndWaitAnswer(byte[] encoded)
        {
            if (m_clientPipeConnection == null)
            {
                throw new Exception("ClientPipeConnection isn't initialized");
            }

            try
            {
                byte[]? returned;

                if (m_async)
                {
                    var token = new CancellationTokenRegistration().Token;
                    var pipe = m_clientPipeConnection as IClientPipeAsync;
                    pipe?.WriteBytes(encoded, token, out var timeSpentInWriteMs);
                    returned = pipe?.ReadBytes(token, out var timeSpentInReadMs);
                }
                else
                {
                    var pipe = m_clientPipeConnection as IClientPipe;
                    pipe?.WriteBytes(encoded, out var timeSpentInWriteMs);
                    returned = pipe?.ReadBytes(out var timeSpentInReadMs);
                }

                if (returned == null)
                {
                    // Log
                    return null;
                }

                var str = Encoding.UTF8.GetString(returned);

                // Deserialize and proceed
                var returnValue = MethodCall.DeserializeReturnValue(str);
                if (returnValue != null)
                {
                    // Log - non-Void (legitimate)
                    var millisecondsReceive = (DateTime.UtcNow - returnValue.UtcSerializedTimestamp).TotalMilliseconds;
                    return returnValue;
                }

                // Log - void (also legitimate)
                return null;
            }
            catch (Exception ex)
            {
                // Log
                throw new InvalidOperationException("Nested: " + ex.Message);
            }
        }

        /// <summary>
        /// Dynamic invocation method. Currently allows only for Reflection based operation (no ability to add methods dynamically).
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            if (args != null)
            {
                try
                {
                    // check instance passed in for methods to invoke
                    if (InvokeMethod(binder, args, out result))
                    {
                        return true;
                    }
                }
                catch (Exception /* ex */)
                {
                    // Log
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Reflection helper method to invoke a method
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private bool InvokeMethod(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            var methodInfo = ((Func<object[], object?>)ProxyStatics.ProxyDelegateObject).Method;
            var argsLength = args?.Length ?? 0;
            object?[] expandedArgs = new object[argsLength + ProxyStatics.ServiceParameters];
            expandedArgs[ProxyStatics.ServiceParameterThis] = this;

            var transferRecord = new TransferRecord
            {
                MethodName = binder.Name,
                ClientPid = Environment.ProcessId,
                ServiceTypeFullName = ""
            };

            expandedArgs[ProxyStatics.ServiceParameterHeader] = transferRecord.SerializeTo();

            if (args != null)
            {
                for (var i = 0; i < argsLength; i++)
                {
                    expandedArgs[i + ProxyStatics.ServiceParameters] = args[i];
                }
            }

            var wrapper = new object[] { expandedArgs };

            result = methodInfo.Invoke(this, wrapper);

            if (wrapper[0] is object[] returnedArgs 
                && 
                args != null 
                && 
                args.Length == returnedArgs.Length - 2) 
            {
                for (int i = 0; i < returnedArgs.Length - ProxyStatics.ServiceParameters; i++)
                {
                    var parameter = (Parameter)returnedArgs[i + ProxyStatics.ServiceParameters];
                    args[i] = parameter.Value;
                }
            }

            return true;
        }
    }
}
