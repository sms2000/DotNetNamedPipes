#define ASYNC_CALLBACK

using ClientServer;
using DataExchangeNET6;

namespace TestServer
{
    public class ServerSideMethodImplementor : IMethodExchange1
    {
        private long m_inCallback4 = 0;
        private long m_outCallback4 = 0;
        private long m_inSkip4 = 0;
        private long m_inCallback6 = 0;
        private long m_outCallback6 = 0;
        private long m_inSkip6 = 0;

        public long CallbackCalled4 => m_inCallback4;
        public long CallbackSkip4 => m_inSkip4;
        public long CallbackCalled6 => m_inCallback6;
        public long CallbackSkip6 => m_inSkip6;

        public static string GetUrl()
        {
            return "my_pipe2";
        }

        #region methods
        public void Method1()
        {
            Console.WriteLine("Method1_1. No params, no return");
        }

        public void Method1(string str1)
        {
            Console.WriteLine("Method1_2. str1={0}, no return", str1);
        }

        public void Method1(string str1, string str2)
        {
            Console.WriteLine("Method1_3 str1={0}, str2={1}, no return", str1, str2);
        }

        public bool Method1(string str1, string str2, string str3)
        {
            var ret = true;
            Console.WriteLine("Method1_4. str1={0}, str2={1}, str3={2}, return={3}", str1, str2, str3, ret);
            return ret;
        }

        public string Method1(string str1, string str2, string str3, string str4)
        {
            var ret = "<retVal>";
            Console.WriteLine("Method1_5. str1={0}, str2={1}, str3={2}, str4={3}, return={4}", str1, str2, str3, str4, ret);
            return ret;
        }

        public string Method1(string str1, string str2, string str3, int int1)
        {
            var ret = "<retVal>";
            Console.WriteLine("Method1_6. str1={0}, str2={1}, str3={2}, int1={3}, return={4}", str1, str2, str3, int1, ret);
            return ret;
        }

        public void Method1(long long1, int int1, string str1)
        {
            Console.WriteLine("Method1_7. long1={0}, int1={1}, str1={2}, no return", long1, int1, str1);
        }

        public string Method2(long[] arrLong1, List<string> listString1)
        {
            var ret = "<retVal>";
            Console.WriteLine("Method2_1. arrLong1={0}, listString1={1}, return={2}", arrLong1, listString1, ret);
            return ret;
        }

        public string Method3(out HashSet<string> hashSet1, ref Dictionary<int, string> dict1)
        {
            var callback4 = OperationContext.Current.GetCallbackChannel<ICallback1>();

            hashSet1 = new HashSet<string>();
            hashSet1.Add("1");
            hashSet1.Add("22");
            hashSet1.Add("333");

            dict1.Add(10, "10101010101010101010");

            var s1 = string.Join(", ", new List<string>(hashSet1));
            var s2 = string.Join(", ", dict1);

            try
            {
                callback4?.Callback1("111111 >>>>>> callback <<<<< 111111");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("Error! Callback1 failed: {0}", ex.Message);
            }

            try
            {
                callback4?.Callback1("222222 >>>>>> callback <<<<< 222222");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("Error! Callback2 failed: {0}", ex.Message);
            }

            var ret = string.Format("[{0}] << >> [{1}]", s1, s2);
            Console.WriteLine(ret);
            return ret;
        }

        public List<string> Method4(ref HashSet<string> hashSet1, out HashSet<string> hashSet2, string str1, char char1, bool bool1, int int1, 
                                    ref Dictionary<int, string> dict1, out Dictionary<int, string> dict2, long long1, out long long2)
        {
            var callback4 = OperationContext.Current.GetCallbackChannel<ICallback1>();

            hashSet2 = new HashSet<string>();
            hashSet2.Add("1");
            hashSet2.Add("22");
            hashSet2.Add("333");

            dict2 = new Dictionary<int, string>();
            dict2.Add(10, "10101010101010101010");
            dict2.Add(20, "20101010101010101020");
            dict2.Add(30, "30101010101010101030");

            var s1 = string.Join(", ", new List<string>(hashSet2));
            var s2 = string.Join(", ", dict2);

#if ASYNC_CALLBACK
            bool callbackOn = false;

            lock (this)
            {
                if (m_inCallback4 <= m_outCallback4)
                {
                    m_inCallback4++;
                    Console.WriteLine("+++++++++ Callback4 called. No race!");
                    callbackOn = true;
                }
                else
                {
                    m_inSkip4++;
                    Console.WriteLine(">>>>>>>>> No callback4 called. Race!");
                }
            }

            if (callbackOn)
            {
                Task.Run(() => executeCallback4(callback4));
            }
#else
            try
            {
                m_callback?.Callback1("111111 >>>>>> callback <<<<< 111111");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("Error! Callback1 failed: {0}", ex.Message);
            }

            try
            {
                m_callback?.Callback1("222222 >>>>>> callback <<<<< 222222");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("Error! Callback2 failed: {0}", ex.Message);
            }
#endif

            var ret = string.Format("[{0}] << >> [{1}]", s1, s2);
#if DEBUG_SERVER
            Console.WriteLine(ret);
#endif

            var returned = new List<string>();
            returned.Add(ret);
            returned.Add(ret);
            returned.Add(ret);

            long2 = 9876543210L;

            return returned;
        }

        public void Method5(out char char1, out short short1, out int int1, out long long1, out float float1, out double double1, out HashSet<string> hashSet1)
        {
            char1 = 'c';
            short1 = 123;
            int1 = 4567;
            long1 = 9876543210L;
            float1 = 123.456f;
            double1 = 1234.5678e11d;

            hashSet1 = new HashSet<string>();
            hashSet1.Add("aaaaaaa");
            hashSet1.Add("bbbbbbb");
            hashSet1.Add("ccccccc");
        }

        public C2STransfer1 Method6(C2STransfer1 inData, out C2STransfer1 outData)
        {
            var callback6 = OperationContext.Current.GetCallbackChannel<ICallback1>();

            var set = inData.GetSet();
            outData = new C2STransfer1(">>> " + inData.GetString(), inData.GetLong() * 1000 + inData.GetLong(), set.ElementAt(2), set.ElementAt(1), set.ElementAt(0));

            var inR = string.Join(".", inData.GetSet().ToArray());
            var outR = string.Join(".", outData.GetSet().ToArray());
            var ret = string.Format("[{0}] << >> [{1}]", inR, outR);

#if DEBUG_SERVER
            Console.WriteLine(ret);
#endif

#if ASYNC_CALLBACK
            bool callbackOn = false;

            lock (this)
            {
                if (m_inCallback6 <= m_outCallback6)
                {
                    m_inCallback6++;
                    callbackOn = true;
                    Console.WriteLine("+++++++++ Callback6 called. No race!");
                }
                else
                {
                    m_inSkip6++;
                    Console.WriteLine(">>>>>>>>> No callback6 called. Race!");
                }
            }

            if (callbackOn)
            {
                Task.Run(() => executeCallback6(callback6));
            }
#endif

            return outData;
        }

#endregion

        private void executeCallback4(ICallback1 callback)
        {
            lock (this)
            {
                try
                {
                    var output = string.Format("111111 >>>>>> callback <<<<< 111111. Callback {0}", callback.GetHashCode());
                    callback.Callback1(output);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Error in Method4! Callback1 failed: {0}", ex.Message);
                }

                m_outCallback4++;
            }
        }

        private void executeCallback6(ICallback1 callback)
        {
            lock (this)
            {
                try
                {
                    var output = string.Format("222222 >>>>>> callback <<<<< 222222. Callback {0}", callback.GetHashCode());
                    callback.Callback1(output, 12345678L);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Error in Metho6! Callback1 failed: {0}", ex.Message);
                }

                m_outCallback6++;
            }
        }
    }
}
