using System.ServiceModel;

namespace ClientServer
{
    [ServiceContract]
    public interface ICallback1
    {
        [OperationContractAttribute]
        public void Callback1();

        [OperationContractAttribute]
        public void Callback1(string str1);

        [OperationContractAttribute]
        public void Callback1(string str1, long long1);
    }
}
