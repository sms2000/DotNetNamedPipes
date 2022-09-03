namespace Interfaces
{
    public interface ISignalProcessor
    {
        public bool ProcessPotentialSignal(byte[] request);
    }
}
