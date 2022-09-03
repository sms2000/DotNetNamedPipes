#nullable enable
using DataExchangeNET6.JsonEngine;
using DataExchangeNET6.Performance;

namespace DataExchangeNET6.Exchange
{
    public class ReturnValue : Parameter
    {
        public DateTime UtcSerializedTimestamp { get; set; }
        public string CustomPayload { get; set; } = string.Empty;
        public List<Parameter?> Parameters { get; set; } = new List<Parameter?>();

        public ReturnValue()
        {
        }

        public ReturnValue(object? baseReturnValue, object?[]? parameters)
        {
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    var param = new Parameter
                    {
                        Value = parameter,
                        TypeName = parameter != null ? parameter.GetType().FullName : null
                    };

                    Parameters.Add(param);
                }
            }

            if (baseReturnValue != null)
            {
                Value = baseReturnValue;
                TypeName = baseReturnValue.GetType().FullName;
            }

            //CorrectValues();
        }

        public new void ReconstuctObject()
        {
            foreach (var parameter in Parameters)
            {
                parameter?.ReconstuctObject();
            }
        }

        public object? ConvertResult()
        {
            if (TypeName != null && Value != null)
            {
                var typeOf = TypeCache.Instance.GetType(TypeName);
                if (typeOf != null)
                {
                    var returnedValue = SerializationHelper.ConvertTo(Value, typeOf);
                    return returnedValue;
                }
            }

            return null;
        }

    }
}
