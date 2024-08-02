using System.Buffers;

namespace HotChocolate.Utilities;

public static class BufferHelper
{
    public static Task<T> ReadAsync<T>(
        Stream stream,
        Func<byte[], int, T> deserialize,
        CancellationToken cancellationToken)
        => ReadAsync(stream, deserialize, static (b, l, d) => d(b, l), cancellationToken);

    public static Task<T> ReadAsync<TState, T>(
        Stream stream,
        TState state,
        Func<byte[], int, TState, T> deserialize,
        CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (deserialize is null)
        {
            throw new ArgumentNullException(nameof(deserialize));
        }

        return ReadAsync(stream, state, int.MaxValue, deserialize, () => { }, cancellationToken);
    }

    public static async Task<T> ReadAsync<TState, T>(
        Stream stream,
        TState state,
        int maxAllowedSize,
        Func<byte[], int, TState, T> deserialize,
        Action throwMaxAllowedError,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        var bytesBuffered = 0;

        try
        {
            while (true)
            {
                var bytesRemaining = buffer.Length - bytesBuffered;

                if (bytesRemaining == 0)
                {
                    var next = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
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

                if (bytesBuffered > maxAllowedSize)
                {
                    throwMaxAllowedError();
                }
            }

            return deserialize(buffer, bytesBuffered, state);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
