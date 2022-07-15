using System.Buffers;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace Testing;

internal sealed class GraphQLSnapshotValueSerializer : ISnapshotValueSerializer
{
    public bool CanHandle(object? value) => value is ISyntaxNode;

    public void Serialize(IBufferWriter<byte> snapshot, object? value)
    {
        if (value is null)
        {
            snapshot.Append(NullValueNode.Default.Print());
            return;
        }

        if (value is ISyntaxNode node)
        {
            var serialized = node.Print().AsSpan();
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
            return;
        }

        throw new InvalidOperationException("Only GraphQL syntax nodes are allowed.");
    }
}
