namespace DataExchangeNET6.Performance
{
    public class TypeCache
    {
        private const int DefaultMaxEntries = 1024;

        private static readonly TypeCache m_instance = new();
        private readonly Dictionary<string, Type> m_cache = new();
        private readonly List<string> m_cacheOrder = new();
        private int m_maxEntries = DefaultMaxEntries;

        public static TypeCache Instance { get { return m_instance; } }

        public static void SetConfiguration(int maxEntries = DefaultMaxEntries)
        {
            lock (m_instance)
            {
                Instance.setConfiguration(maxEntries);
            }
        }

        public Type? GetType(string name)
        {
            lock (m_instance)
            {
                if (m_cacheOrder.Contains(name))
                {
                    m_cacheOrder.Remove(name);
                    m_cacheOrder.Add(name);

#if DEBUG_CACHE
                    Console.WriteLine("Cache hit for: {0}", name);
#endif

                    // Log
                    return m_cache[name];
                }
                else
                {
                    var type = Type.GetType(name);
                    if (type == null)
                    {
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            type = asm.GetType(name);
                            if (type != null)
                            {
                                break;
                            }
                        }
                    }

                    if (type == null)
                    {
#if DEBUG_CACHE
                        Console.WriteLine("Error: no type available for: {0}", name);
#endif

                        // Log
                        return null;
                    }
                    else
                    {
                        if (m_cacheOrder.Count >= m_maxEntries)
                        {
                            var removedTypeName = m_cacheOrder[0];
                            m_cacheOrder.RemoveAt(0);
                            m_cache.Remove(removedTypeName);

#if DEBUG_CACHE
                            Console.WriteLine("Reached the max of the Type cache entries. The oldest cache hit removed: {0}", removedTypeName);
#endif

                            // Log
                        }

                        m_cache.Add(name, type);
                        m_cacheOrder.Add(name);

#if DEBUG_CACHE
                        Console.WriteLine("Added to Type cache. Total: {0}", m_cache.Count);
#endif

                        // Log
                        return type;
                    }
                }
            }
        }

#region private
        /// <summary>
        /// No direct constructing, only through the Instance
        /// [Singleton]
        /// </summary>
        private TypeCache()
        {
        }

        private void setConfiguration(int maxEntries)
        {
            if (m_maxEntries != maxEntries)
            {
                m_maxEntries = maxEntries;
                m_cache.Clear();
            }
        }
#endregion private
    }
}
