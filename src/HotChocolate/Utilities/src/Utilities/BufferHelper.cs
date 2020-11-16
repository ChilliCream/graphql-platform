using System.Threading;
using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace HotChocolate.Utilities
{
    public static class BufferHelper
    {
        public static Task<T> ReadAsync<T>(
           Stream stream,
           Func<byte[], int, T> handle,
           CancellationToken cancellationToken) =>
           ReadAsync(stream, handle, null, cancellationToken);

        public static async Task<T> ReadAsync<T>(
            Stream stream,
            Func<byte[], int, T> handle,
            Action<int>? checkSize,
            CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(1024);
            var bytesBuffered = 0;

            try
            {
                while (true)
                {
                    var bytesRemaining = buffer.Length - bytesBuffered;

                    if (bytesRemaining == 0)
                    {
                        byte[] next = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                        Buffer.BlockCopy(buffer, 0, next, 0, buffer.Length);
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = next;
                        bytesRemaining = buffer.Length - bytesBuffered;
                    }

                    var bytesRead = await stream.ReadAsync(
                        buffer,
                        bytesBuffered,
                        bytesRemaining,
                        cancellationToken)
                        .ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    bytesBuffered += bytesRead;
                    checkSize?.Invoke(bytesBuffered);
                }

                return handle(buffer, bytesBuffered);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
