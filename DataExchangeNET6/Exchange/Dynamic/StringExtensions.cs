using System.Globalization;

namespace DataExchangeNET6.Exchange.Dynamic
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts the specified name to camel case.
        /// </summary>
        /// <param name="name">The name to convert</param>
        /// <returns>A camel cased version of the specified name</returns>
        public static string ToCamelCase(this string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            return string.Join(".", name.Split('.').Select(n => char.ToLower(n[0], CultureInfo.InvariantCulture) + n.Substring(1)));
        }
    }
}
