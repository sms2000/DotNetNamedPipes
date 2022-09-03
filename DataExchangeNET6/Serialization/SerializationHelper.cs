using System.Text.Json;
using Interfaces;

namespace DataExchangeNET6.JsonEngine
{
    public sealed class SerializationHelper
    {
        private static readonly JsonSerializerOptions m_options = new()
        { 
            IncludeFields = true 
        };

        public static string SerializeObject(object obj)
        {
            var str = JsonSerializer.Serialize(obj, obj.GetType(), m_options);
            return str;
        }

        public static T? DeserializeObject<T>(string json) where T : ISerializeable
        {
            var obj = JsonSerializer.Deserialize<T>(json, m_options);
            obj?.ReconstuctObject();
            return obj;
        }

        public static object? ConvertTo(object value, Type typeOf)
        {
            var toJson = JsonSerializer.Serialize(value, value.GetType(), m_options);
            var toTypeOf = JsonSerializer.Deserialize(toJson, typeOf, m_options);
            return toTypeOf;
        }
    }
}
