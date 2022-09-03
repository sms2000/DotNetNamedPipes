using System.Reflection;
using System.Text;
using Interfaces;

namespace DataExchangeNET6.Exchange
{
    public class DataExchangeProcessor : DataExchangeHelper, IDataExchangeProcessor
    {
        /// <summary>
        /// Do request-response style data exchange
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="processor"></param>
        /// <param name="pipe"></param>
        /// <returns></returns>
        public virtual byte[]? DoExchange<T>(byte[] request, T processor, IConnection pipe) where T : class
        {
            var str = Encoding.UTF8.GetString(request);
            var methodCall = MethodCall.DeserializeMethodCall(str);
            if (methodCall != null)
            {
                var callbackProcessor = pipe.GetCallbackProcessor();
                if (callbackProcessor != null) 
                {
                    OperationContext.PopulateCurrentContext(callbackProcessor);
                    // Log
                }
                else
                {
                    // Log
                }

                MethodInfo? methodInfo = methodCall.FindAppropriateMethod(processor);
                if (methodInfo != null)
                {
                    // 1. Decide to process
                    var parameters = MethodCall.ConvertParameters(methodCall.Parameters);
                    var returnValue = methodInfo.Invoke(processor, parameters);

                    // 2. Decide on ByRef & Out
                    var genericReturnValue = new ReturnValue(returnValue, parameters);

                    // 3. Drop [In] parameters
                    filterOffInParameters(genericReturnValue, methodInfo.GetParameters());

                    // 4. Free context
                    if (callbackProcessor != null)
                    {
                        OperationContext.FreeCurrentContent(callbackProcessor);
                    }

                    // 5. Return
                    var serialized = MethodCall.SerializeResultValue(genericReturnValue);
                    return Encoding.UTF8.GetBytes(serialized);
                }
            }

            return null;
        }
    }
}
