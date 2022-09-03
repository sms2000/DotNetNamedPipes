using System.Reflection;
using System.Text;
using Interfaces;

namespace DataExchangeNET6.Exchange
{
    public class MethodExchangeProcessorAsync : DataExchangeHelper, IDataExchangeProcessorAsync
    {
        public Task DoExchangeAsync<T>(byte[] request, List<byte[]> response, T processor, IConnection pipe, CancellationToken? token = null) where T : class
        {
            var tasks = new List<Task>();
            var task = new Task(() => doExchangeAsyncTask(request, response, processor, pipe, token, tasks));
            tasks.Add(task);
            task.Start();
            return task;
        }

        #region private
        private void doExchangeAsyncTask<T>(byte[] request, List<byte[]> response, T processor, IConnection pipe, CancellationToken? token, List<Task> tasks) where T : class
        {
            response.Clear();

            var str = Encoding.UTF8.GetString(request);
            var methodCall = MethodCall.DeserializeMethodCall(str);
            if (methodCall != null)
            {
                MethodInfo? methodInfo = methodCall.FindAppropriateMethod(processor);
                if (methodInfo != null)
                {
                    // 1. Decide to process
                    var parameters = MethodCall.ConvertParameters(methodCall.Parameters);

                    if (token is { IsCancellationRequested: true })
                    {
                        return;
                    }

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

                    var returnValue = methodInfo.Invoke(processor, parameters);

                    if (token is { IsCancellationRequested: true })
                    {
                        return;
                    }

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
                    response.Add(Encoding.UTF8.GetBytes(serialized));
                }
            }
        }
        #endregion
    }
}
