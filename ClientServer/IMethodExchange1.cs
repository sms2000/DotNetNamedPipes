using System.ServiceModel;

namespace ClientServer
{
    [ServiceContract(CallbackContract = typeof(ICallback1))]
    public interface IMethodExchange1
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

        [OperationContractAttribute]
        List<string> Method4(ref HashSet<string> hashSet1, out HashSet<string> hashSet2, string str1, char char1, bool bool1, int int1, ref Dictionary<int, string> dict1, out Dictionary<int, string> dict2, long long1, out long long2);

        [OperationContractAttribute]
        void Method5(out char char1, out short short1, out int int1, out long long1, out float float1, out double double1, out HashSet<string> hashSet1);

        [OperationContractAttribute]
        C2STransfer1 Method6(C2STransfer1 inData, out C2STransfer1 outData);
    }
}
