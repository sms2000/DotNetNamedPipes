#nullable enable
using System.Reflection;
using System.Text.RegularExpressions;
using DataExchangeNET6.JsonEngine;
using DataExchangeNET6.Performance;
using Interfaces;

namespace DataExchangeNET6.Exchange
{
    public class MethodCall : ISerializeable
    {
        private static readonly Regex m_byRefSplit = new(@"([^&]*)\&.*", RegexOptions.Compiled);
        
        public List<Parameter> Parameters { get; set; } = new();
        public string? Name { get; set; }
        public int ClientPid { get; set; }
        public DateTime UtcSerializedTimestamp { get; set; }
        public string CustomPayload { get; set; } = string.Empty;

        public MethodCall()
        {
        }

        public MethodCall(TransferRecord transferRecord)
        {
            Name = transferRecord.MethodName;
            ClientPid = transferRecord.ClientPid;
        }

        public void AddParameter(object? value)
        {
            var parameter = new Parameter
            {
                Value = value,
                TypeName = value?.GetType().FullName
            };

            Parameters.Add(parameter);
        }

        public string SerializeMethodCall()
        {
            UtcSerializedTimestamp = DateTime.UtcNow;
            return SerializationHelper.SerializeObject(this);
        }

        public static MethodCall? DeserializeMethodCall(string json)
        {
            try
            {
                MethodCall? newObject = SerializationHelper.DeserializeObject<MethodCall>(json);

                if (newObject != null)
                {
                    foreach (var parameter in newObject.Parameters)
                    {
                        parameter?.ReconstuctObject();
                    }
                }

                return newObject;
            }
            catch (Exception /* ex */)
            {
                // Log
                return default;
            }
        }

        public static string SerializeResultValue(ReturnValue? returnValue)
        {
            if (returnValue != null)
            {
                returnValue.UtcSerializedTimestamp = DateTime.UtcNow;
                return SerializationHelper.SerializeObject(returnValue);
            }

            return string.Empty;
        }

        public static ReturnValue? DeserializeReturnValue(string json)
        {
            try
            {
                ReturnValue? newObject = SerializationHelper.DeserializeObject<ReturnValue>(json);
                newObject?.ReconstuctObject();
                return newObject;
            }
            catch (Exception /* ex */)
            {
                // Log
                return default;
            }
        }

        public static object?[] ConvertParameters(List<Parameter> parameters)
        {
            var length = parameters.Count;
            object?[] inputParameters = new object[length];

            for (int i = 0; i < length; i++)
            {
                var parameter = parameters[i];
                if (parameter.TypeName != null && parameter.Value != null) 
                {
                    var parameterType = TypeCache.Instance.GetType(parameter.TypeName);
                    if (parameterType != null) 
                    {
                        inputParameters[i] = parameter.Value;
                        continue;
                    }
                }

                inputParameters[i] = null;
            }

            return inputParameters;
        }

        public MethodInfo? FindAppropriateMethod<T>(T processor)
        {
            if (processor == null)
            {
                return null;
            }

            var methods = processor.GetType().GetMethods();
            foreach (var mi in methods)
            {
                if (mi.Name != Name || mi.GetParameters().Length != Parameters.Count)
                {
                    continue;
                }

                var length = mi.GetParameters().Length;
                var i = 0;

                for (; i < length; i++)
                {
                    var type2Check = mi.GetParameters()[i];
                    var typeName = Parameters[i].TypeName;

                    if (typeName == null)
                    {
                        continue;
                    }

                    var type = TypeCache.Instance.GetType(typeName);
                    if (type == default)
                    {
                        break;
                    }

                    var typeName4Check = type.Name;
                    if (typeName4Check != null && type2Check.ParameterType.IsByRef)
                    {
                        if (!compatibleByRef(type2Check.ParameterType.Name, typeName4Check))
                        {
                            break;
                        }
                    }
                    else if (!type2Check.ParameterType.IsAssignableFrom(type))
                    {
                        // Not OK
                        break;
                    }
                }

                if (i == length)
                {
                    return mi;
                }
            }

            return null;
        }

        public void ReconstuctObject()
        {
        }

        #region private
        private static bool compatibleByRef (string type2Check, string baseType)
        {
            Match matcher = m_byRefSplit.Match(type2Check);
            if (matcher.Success)
            {
                if (matcher.Groups[1].Value.StartsWith(baseType))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion
    }
}
