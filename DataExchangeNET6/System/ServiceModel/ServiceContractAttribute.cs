namespace System.ServiceModel
{
    public sealed class ServiceContractAttribute : Attribute
    {
        Type? callbackContract = null;

        public Type? CallbackContract
        {
            get { return callbackContract; }
            set { callbackContract = value; }
        }
    }
}
