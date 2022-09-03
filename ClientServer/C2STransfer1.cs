using System.Text.Json.Serialization;

namespace ClientServer
{
    public class C2STransfer1
    {
        public string m_string;

        public long m_long;

        public HashSet<string> m_hashSet = new HashSet<string>();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        /// <summary>
        /// Serialization constructor - mandatory to have
        /// </summary>
        public C2STransfer1()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        /// <summary>
        /// Helper constructor
        /// </summary>
        /// <param name="_string"></param>
        /// <param name="_long"></param>
        /// <param name="set1"></param>
        /// <param name="set2"></param>
        /// <param name="set3"></param>
        public C2STransfer1(string _string, long _long, string set1, string set2, string set3)
        {
            m_string = _string;
            m_long = _long;
            m_hashSet.Add(set1);
            m_hashSet.Add(set2);
            m_hashSet.Add(set3);
        }

        public string GetString()
        {
            return m_string;
        }

        public long GetLong()
        {
            return m_long;
        }

        public HashSet<string> GetSet()
        {
            return m_hashSet;
        }
    }
}
