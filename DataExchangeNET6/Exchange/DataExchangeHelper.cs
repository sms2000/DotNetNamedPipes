using System.Reflection;

namespace DataExchangeNET6.Exchange
{
    public class DataExchangeHelper
    {
        protected static void filterOffInParameters(ReturnValue returnValue, ParameterInfo[] parameterInfos)
        {
            for (int i = 0; i < parameterInfos.Length; i++)
            {
                var parameterInfo = parameterInfos[i];
                if (!parameterInfo.ParameterType.IsByRef)
                {
                    returnValue.Parameters[i] = null;
                }
            }
        }
    }
}
