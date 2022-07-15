using System.Buffers;

namespace Testing;

internal sealed class PlainTextSnapshotValueSerializer : ISnapshotValueSerializer
{
    public bool CanHandle(object? value)
        => value is string;

    public void Serialize(IBufferWriter<byte> snapshot, object? value)
    {
        if (value?.ToString() is { } s)
        {
            var serialized = s.AsSpan();
            var buffer = ArrayPool<char>.Shared.Rent(serialized.Length);
            var span = buffer.AsSpan()[..serialized.Length];
            var written = 0;

            for (var i = 0; i < serialized.Length; i++)
            {
                if (serialized[i] is not '\r')
                {
                    span[written++] = serialized[i];
                }
            }

            span = span[..written];
            snapshot.Append(span);

            ArrayPool<char>.Shared.Return(buffer);
            snapshot.Append(s);
        }
    }
}
