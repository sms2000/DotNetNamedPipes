using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Interfaces;
using DataExchangeNET6.Exchange.Dynamic;

namespace DataExchangeNET6.Exchange
{
    public static class ProxyStatics
    {
        public const int ServiceParameters = 2;
        public const int ServiceParameterThis = 0;
        public const int ServiceParameterHeader = 1;

#region delegates
        public static object? ProxyDelegateObject(params object?[] pp)
		{
            var returnVal = processDelegate(pp);
            return returnVal;
		}

        public static MethodInfo GetProxyDelegateMethodInfo()
        {
            return typeof(ProxyStatics).GetMethod("ProxyDelegateObject", new[] { typeof(object[]) }) ?? throw new InvalidOperationException("Method 'ProxyStatics::ProxyDelegateObject' not found");
        }

#endregion

#region private
        private static object? processDelegate(object?[] pp)
        {
            if (pp.Length < ServiceParameters || pp[ServiceParameterThis] == null || pp[ServiceParameterHeader] == null)
            {
                throw new ArgumentException("Both '_this' and 'methodName' must exist");
            }

            MethodCall? methodCall = null;

#pragma warning disable CS8604 // Possible null reference argument.
            var thisObj = DynamicPropertiesHelper.GetDynamicProperty(pp[ServiceParameterThis], IPCHelper.DynamicPropertyName) as IDynamicObject;
#pragma warning restore CS8604 // Possible null reference argument.

            if (pp[ServiceParameterHeader] is string json)
            {
                var transferRecord = TransferRecord.Reconstruct(json);
                if (transferRecord == null)
                {
                    throw new SerializationException(json + " cannot be deserialized into TransferRecord");
                }

                methodCall = new MethodCall(transferRecord);
            }

            if (methodCall == null || thisObj == null)
            {
                throw new ArgumentException("'_this' or 'methodName'");
            }

            for (var i = ServiceParameters; i < pp.Length; i++)
            {
                methodCall.AddParameter(pp[i]);
            }

            var serialized = methodCall.SerializeMethodCall();
            var encoded = Encoding.UTF8.GetBytes(serialized);

            var returnValueObj = thisObj.SendAndWaitAnswer(encoded);
            if (returnValueObj != null)
            {
                if (returnValueObj is ReturnValue returnValue)
                {
                    for (var i = 0; i < returnValue.Parameters.Count; i++)
                    {
                        pp[i + ServiceParameters] = returnValue.Parameters[i]?.Value;
                    }

                    var converted = returnValue.ConvertResult();
                    return converted;
                }

            }

            return null;
        }
#endregion
	}
}
