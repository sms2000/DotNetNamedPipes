using System.Text;
using System.Reflection;
using Interfaces;

namespace DataExchangeNET6.Exchange
{
    public class DataExchangeProcessorAsync : DataExchangeHelper, IDataExchangeProcessorAsync
    {
        public Task DoExchangeAsync<T>(byte[] request, List<byte[]> response, T processor, IConnection pipe, CancellationToken? token = null) where T : class
        {
            var task = new Task(() => doExchangeAsyncTask(request, response, processor, pipe, token));
            task.Start();
            return task;
        }

        #region private
        private void doExchangeAsyncTask<T>(byte[] request, List<byte[]> response, T processor, IConnection pipe, CancellationToken? token) where T : class
        {
            response.Clear();

            if (token is { IsCancellationRequested: true })
            {
                return;
            }

            var str = Encoding.UTF8.GetString(request);
            var methodCall = MethodCall.DeserializeMethodCall(str);

            if (token is { IsCancellationRequested: true })
            {
                return;
            }

            if (methodCall == null)
            {
                // Log
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

            if (token is { IsCancellationRequested: true })
            {
                return;
            }

            var methodInfo = methodCall.FindAppropriateMethod(processor);

            if (token is { IsCancellationRequested: true })
            {
                return;
            }

            if (methodInfo == null)
            {
                // Log
                return;
            }

            // 1. Decide to process
            var parameters = MethodCall.ConvertParameters(methodCall.Parameters);
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

            if (token is { IsCancellationRequested: true })
            {
                return;
            }

            response.Add(Encoding.UTF8.GetBytes(serialized));
        }
        #endregion
    }
}
