namespace DataExchangeNET6.Pipes
{
    /// <summary>
    /// Fired when the inflate process fails
    /// </summary>
    public class DecompressException : Exception
    {
        public DecompressException(string? message) : base(message)
        {
        }
    }
}
