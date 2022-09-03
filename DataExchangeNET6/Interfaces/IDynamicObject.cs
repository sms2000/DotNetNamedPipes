namespace Interfaces
{
    /// <summary>
    /// Dynamic object exchange interface
    /// </summary>
    public interface IDynamicObject : IDisposable
    {
        /// <summary>
        /// Two-way synchronous exchange
        /// </summary>
        /// <param name="encoded">Binary blob</param>
        /// <returns>Optional returned object</returns>
        public object? SendAndWaitAnswer(byte[] encoded);
    }
}
