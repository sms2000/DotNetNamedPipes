using ClientServer;
using DataExchangeNET6;

namespace TestServer
{
    public class ServerSideImplementor : IDataExchange1
    {
        public static string GetUrl()
        {
            return "my_pipe";
        }

        #region acceptors
        public void Method1()
        {
        }

        public void Method1(string str1)
        {
            try
            {
                ICallback1? callback1 = OperationContext.Current.GetCallbackChannel<ICallback1>();
                callback1?.Callback1($"Length = {str1.Length}");
            }
            catch (InvalidOperationException)
            {
            }
        }

        public void Method1(string str1, string str2)
        {
        }

        public bool Method1(string str1, string str2, string str3)
        {
            return true;
        }

        public string Method1(string str1, string str2, string str3, string str4)
        {
            return "<wrong>";
        }

        public string Method1(string str1, string str2, string str3, int int1)
        {
            return "<wrong>";
        }

        public void Method1(long long1, int int1, string str1)
        {
            var output = string.Format("{0} == {1} == {2}", long1, int1, str1);
            output.ToString();
        }

        public string Method2(long[] arrLong1, List<string> listString1)
        {
            var s1 = string.Join(", ", arrLong1);
            var s2 = string.Join(", ", listString1);

            return string.Format("[{0}] << >> [{1}]", s1, s2);
        }

        public string Method3(out HashSet<string> hashSet1, ref Dictionary<int, string> dict1)
        {
            hashSet1 = new HashSet<string>();
            hashSet1.Add("1");
            hashSet1.Add("22");
            hashSet1.Add("333");

            dict1.Add(10, "10101010101010101010");

            var s1 = string.Join(", ", new List<string> (hashSet1));
            var s2 = string.Join(", ", dict1);

            try
            {
                ICallback1? callback1 = OperationContext.Current.GetCallbackChannel<ICallback1>();
                callback1?.Callback1("111111 >>>>>> callback <<<<< 111111");
            }
            catch(InvalidOperationException)
            {
                Console.WriteLine("Error! Callback1 failed");
            }

            try
            {
                ICallback1? callback2 = OperationContext.Current.GetCallbackChannel<ICallback1>();
                callback2?.Callback1("222222 >>>>>> callback <<<<< 222222");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("Error! Callback2 failed");
            }

            return string.Format("[{0}] << >> [{1}]", s1, s2);
        }

        #endregion

        #region private
        #endregion
    }
}
