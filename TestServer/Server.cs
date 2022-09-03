using ClientServer;
using DataExchangeNET6.Exchange;
using PipeSecurityHelper;

namespace TestServer
{
    public static class Server
    {
        private const int MaxConcurrentClients = 4; // OK for tests

        static void Main(string[] args)
        {
            var exitEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, eventArgs) => 
            {
                Console.WriteLine("Ctrl+C caught. Finishing...");
                eventArgs.Cancel = true;
                exitEvent.Set();
            };


            Console.WriteLine("Starting the Server...");
            var implementor = new ServerSideMethodImplementor();
            var host = IPCHelper.CreateAndOpenServiceHost<IMethodExchange1>(ServerSideMethodImplementor.GetUrl(), implementor,
                                                                            MaxConcurrentClients, PipeSecurityProvider.SecurityLimitations.User);

            Console.WriteLine("The Server has started.");
            exitEvent.WaitOne();

            IPCHelper.Close(host);

            Console.WriteLine("Server has finished.\nStatistics:");

            Console.WriteLine("Callback4: called {0} times, but had to skip {1} times", implementor.CallbackCalled4, implementor.CallbackSkip4);
            Console.WriteLine("Callback6: called {0} times, but had to skip {1} times", implementor.CallbackCalled6, implementor.CallbackSkip6);
        }
    }
}
