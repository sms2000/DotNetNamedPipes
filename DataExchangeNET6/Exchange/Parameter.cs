using DataExchangeNET6.JsonEngine;
using DataExchangeNET6.Performance;
using Interfaces;

namespace DataExchangeNET6.Exchange
{
    public class Parameter : ISerializeable
    {
        public string? TypeName { get; set; }
        public object? Value { get; set; }

        public Parameter()
        {
            Value = null;
            TypeName = null;
        }

        public virtual void ReconstuctObject()
        {
            if (Value != null && TypeName != null)
            {
                var typeOf = TypeCache.Instance.GetType(TypeName);
                if (typeOf != null)
                {
                    var value2 = SerializationHelper.ConvertTo(Value, typeOf);
                    Value = value2;

                    //Log
                    return;
                }
            }

            // Log
        }
    }
}
