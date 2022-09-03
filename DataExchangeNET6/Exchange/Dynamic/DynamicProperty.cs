using DataExchangeNET6.Performance;

namespace DataExchangeNET6.Exchange.Dynamic
{
    /// <summary>
    /// Represents the definition of a dynamic property which can be added to an object at runtime.
    /// </summary>
    public class DynamicProperty
    {
        /// <summary>
        /// The Name of the underlying System Type of the property.
        /// </summary>
        private string SystemTypeName { get; set; }

        /// <summary>
        /// The Name of the property.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// The Display Name of the property for the end-user.
        /// </summary>
        public string DisplayName { get; set; }

        public DynamicProperty(string propertyName, string displayName, string systemTypeName = "System.Object")
        {
            PropertyName = propertyName;
            DisplayName = displayName;
            SystemTypeName = systemTypeName;
        }

        /// <summary>
        /// Returns the type
        /// Note: keep method, not property to deal with JSON
        /// </summary>
        /// <returns></returns>
        public Type? GetSystemType()
        {
            return TypeCache.Instance.GetType(SystemTypeName);
        }
    }
}
