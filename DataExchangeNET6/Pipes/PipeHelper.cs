using System.IO.Pipes;
using DataExchangeNET6.Pipes;
using DataExchangeNET6.Transport;

namespace DataExchangeNET6
{
    public abstract class PipeHelper<T> : PipeCompression
    {
        protected abstract int GetMinimalLengthToCompress();

        #region steps
        protected byte[]? ReadAsyncStep(PipeStream stream, CancellationToken token)
        {
            byte[] buffer = new byte[TransportHeader.HeaderLength];

            var readerTasks = new Task[2];
            readerTasks[0] = reader(stream, buffer, token);
            readerTasks[1] = readerTasks[0].ContinueWith(task => FinishedRead(task, readerTasks, buffer), token);

            try
            {
                Task.WhenAll(readerTasks).Wait(token);
                // Log
            }
            catch (OperationCanceledException)
            {
                // Log
                return default;
            }
            catch (AggregateException)
            {
                // Log
                return default;
            }

#if DEBUG_SERVER
            Console.WriteLine("Reading from pipe {0}, {1} bytes", stream.GetHashCode(), buffer.Length);
#endif

            var transportHeader = TransportHeader.Reconstruct(buffer);
            if (transportHeader.PayloadLength < 1)
            {
                // Log
                return default;
            }

            buffer = new byte[transportHeader.CompressedLength];

            readerTasks = new Task[2];
            readerTasks[0] = reader(stream, buffer, token);
            readerTasks[1] = readerTasks[0].ContinueWith(task => FinishedRead(task, readerTasks, buffer), token);

            try
            {
                Task.WhenAll(readerTasks).Wait(token);
                // Log
            }
            catch (OperationCanceledException)
            {
                // Log
                return default;
            }
            catch (AggregateException)
            {
                // Log
                return default;
            }

#if DEBUG_SERVER
            Console.WriteLine("Reading from pipe {0}, {1} bytes", stream.GetHashCode(), buffer.Length);
#endif

            try
            {
                buffer = PostProcessDataAfterRead(buffer, ref transportHeader);
                return buffer;
            }
            catch (DecompressException)
            {
                // Log
#if DEBUG
                Console.WriteLine("The pipe {0} experienced a fault during inflate", stream.GetHashCode());
#endif

                return default;
            }
        }

        protected void FinishedRead(Task task, Task[] tasks, byte[] buffer)
        {
            // This method is good primary for logs
            // Log
        }

        protected void ReceivedData(byte[] buffer)
        {
            // Log
        }

        protected bool WriteAsyncStep(PipeStream stream, byte[] buffer, CancellationToken token)
        {
            var transportHeader = new TransportHeader(buffer.Length);

            buffer = PreProcessDataForWrite(buffer, ref transportHeader, GetMinimalLengthToCompress());
            var rawHeader = transportHeader.Serialize();

            var writerTasks = new Task[2];
            writerTasks[0] = writer(stream, rawHeader, token);
            writerTasks[1] = writerTasks[0].ContinueWith(task => FinishedWrite(task, writerTasks, rawHeader), token);

            try
            {
                Task.WhenAll(writerTasks).Wait(token);
                // Log
            }
            catch (AggregateException)
            {
                // Log
                return false;
            }

            writerTasks = new Task[2];
            writerTasks[0] = writer(stream, buffer, token);
            writerTasks[1] = writerTasks[0].ContinueWith(task => FinishedWrite(task, writerTasks, buffer), token);

            try
            {
                Task.WhenAll(writerTasks).Wait(token);    
                // Log
            }
            catch (OperationCanceledException)
            {
                // Log
                return default;
            }
            catch (AggregateException)
            {
                // Log
                return false;
            }

            return true;
        }

        protected void FinishedWrite(Task task, Task[] tasks, byte[] buffer)
        {
            // This method is good primary for logs
            // Log
        }
        #endregion

        #region private
        private static async Task reader(PipeStream serverStream, byte[] buffer, CancellationToken token)
        {
            var returned = await serverStream.ReadAsync(buffer, token);
            if (returned != buffer.Length)
            {
#if DEBUG
                Console.WriteLine("Error: 'reader'. Read {0} bytes, but intended {1} bytes", returned, buffer.Length);
#endif
                // Log
            }
        }

        private static async Task writer(PipeStream serverStream, byte[] buffer, CancellationToken token)
        {
            await serverStream.WriteAsync(buffer, token);
        }
        #endregion
    }
}
