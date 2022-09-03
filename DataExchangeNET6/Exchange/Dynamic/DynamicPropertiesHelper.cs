namespace DataExchangeNET6.Exchange.Dynamic
{ 
    public static class DynamicPropertiesHelper
    {
        public static void SetDynamicProperty(object host, string propertyName, object? value)
        {
            var property = host.GetType().GetProperty(propertyName);
            if (property != null)
            {
                var set = property.GetSetMethod();
                if (set != null)
                {
                    set.Invoke(host, new[] {value});
                    return;
                }
            }

            // Log
        }

        public static object? GetDynamicProperty(object host, string propertyName)
        {
            var property = host.GetType().GetProperty(propertyName);
            if (property != null)
            {
                var get = property.GetGetMethod();
                if (get != null)
                {
                    return get.Invoke(host, Array.Empty<object>());
                }
            }

            // Log
            return default;
        }
    }
}
