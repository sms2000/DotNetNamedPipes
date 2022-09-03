using ClientServer;

namespace TestClient
{
    internal class CallbackProcessor : ICallback1
    {
        public void Callback1()
        {
            Console.WriteLine("Callback1 - 0");
        }

        public void Callback1(string str1)
        {
            Console.WriteLine("Callback1 - 1: " + str1);
        }

        public void Callback1(string str1, long long1)
        {
            Console.WriteLine("Callback1 - 2: " + str1 + " [" + long1 + "]");
        }
    }
}
