using System.Reflection;
using DataExchangeNET6.JsonEngine;
using Interfaces;

namespace DataExchangeNET6.Exchange
{
    public class TransferRecord : ISerializeable
    {
        public string MethodName { get; init; } = string.Empty;
        public string ServiceTypeFullName { get; init; } = string.Empty;
        public int ClientPid { get; init; }

        public static MethodInfo GetBuildSerializedMethodInfo()
        {
            return typeof(TransferRecord).GetMethod("BuildSerialized", new Type[] { typeof(String) }) ?? throw new InvalidOperationException("Method 'TransferRecord::BuildSerialized' not found");
        }

        public static string BuildSerialized(string methodName)
        {
            var record = new TransferRecord
            {
                MethodName = methodName,
                ClientPid = Environment.ProcessId,
                ServiceTypeFullName = string.Empty
            };

            return record.SerializeTo();
        }

        public string SerializeTo()
        {
            return SerializationHelper.SerializeObject(this);
        }

        public static TransferRecord? Reconstruct(string json)
        {
            try
            {
                TransferRecord? newObject = SerializationHelper.DeserializeObject<TransferRecord>(json);
                return newObject;
            }
            catch (Exception /* ex */)
            {
                // Log
                return default;
            }
        }

        public void ReconstuctObject()
        {
            // Do nothing
        }
    }
}
