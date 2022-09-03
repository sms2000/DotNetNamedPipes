using System.ServiceModel;

namespace ClientServer
{
    [ServiceContract(CallbackContract = typeof(ICallback1))]
    public interface IDataExchange1
    {
        [OperationContractAttribute]
        void Method1();

        [OperationContractAttribute]
        void Method1(string str1);

        [OperationContractAttribute]
        void Method1(string str1, string str2);
        
        [OperationContractAttribute]
        bool Method1(string str1, string str2, string str3);
        
        [OperationContractAttribute]
        string Method1(string str1, string str2, string str3, string str4);

        [OperationContractAttribute]
        string Method1(string str1, string str2, string str3, int int1);

        [OperationContractAttribute]
        void Method1(long long1, int int1, string str1);

        [OperationContractAttribute]
        string Method2(long[] arrLong1, List<string> listString1);

        [OperationContractAttribute]
        string Method3(out HashSet<string> hashSet1, ref Dictionary<int, string> dict1);
    }
}