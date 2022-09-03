using ClientServer;
using DataExchangeNET6.Exchange;

namespace TestClient
{
    public static class Client
    {
        static void Main(string[] args)
        {
            var callbackProcessor = new CallbackProcessor();
            var exitEvent = new ManualResetEvent(false);
            
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("Ctrl+C caught. Finishing...");
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            IMethodExchange1 clientPipe2;
            try
            {
                var now = DateTime.UtcNow;
                clientPipe2 = IPCHelper.CreateDuplexChannel<IMethodExchange1>("my_pipe2", callbackProcessor, 1000);
                Console.WriteLine("CreateDupleChannel finished in {0} ms", (DateTime.UtcNow - now).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return;
            }

            for (int i = 0; i < 1000 && !exitEvent.WaitOne(0); i++)
            {
                var hashSet1 = new HashSet<string>
                {
                    "AAAAAAAAAAAAA"
                };

                var dict1 = new Dictionary<int, string>
                {
                    { 100, "100" }
                };

                var now = DateTime.UtcNow;
                var result = clientPipe2.Method4(ref hashSet1, out var hashSet2, "ssssssssss", 'v', true, 123, ref dict1, out var dict2, 12345L, out var long1);
                var ms = (DateTime.UtcNow - now).TotalMilliseconds;

                try
                {
                    Console.WriteLine("{0,7} {1,7} ms. Method4 called. Returned = {2}, hashSet1 = {3}, dict1 = {4} long1 = {5}",
                                      i, ms, string.Join("", result.ToArray()), string.Join("", hashSet2.ToArray()), string.Join("", dict2.ToArray()), long1);
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("NullReferenceException as a reason to stop...");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: {0,7} Method4 called. Error with ret/out/ref values!", i);
                    Console.WriteLine("Exception: " + ex.Message + Environment.NewLine + "Stack: " + ex.StackTrace);
                }

                var inData = new C2STransfer1("AAAAAAAAAA", 456, "a1", "b2", "c3");

                now = DateTime.UtcNow;
                var result1 = clientPipe2.Method6(inData, out var outData);
                ms = (DateTime.UtcNow - now).TotalMilliseconds;

                try
                {
                    Console.WriteLine("{0,7} {1,7} ms. Method6 called. Returned = {2}, outData = {3}", i, ms, result1.GetString(), string.Join("", outData.GetSet().ToArray()));
                }
                catch(NullReferenceException)
                {
                    Console.WriteLine("NullReferenceException as the reason to stop...");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: {0,7} Method6 called. Error with ret/out values!", i);
                    Console.WriteLine("Exception: " + ex.Message + Environment.NewLine + "Stack: " + ex.StackTrace);
                }

                Thread.Sleep(1000);
            }

            IPCHelper.Close(clientPipe2);
            Console.WriteLine("End.");
        }
    }
}
