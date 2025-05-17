using System.Buffers;
using CookieCrumble.Formatters;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace CookieCrumble.HotChocolate.Formatters;

internal sealed class GraphQLSnapshotValueFormatter : SnapshotValueFormatter<ISyntaxNode>
{
    protected override void Format(IBufferWriter<byte> snapshot, ISyntaxNode value)
    {
        var serialized = value.Print().AsSpan();
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
    }

    protected override void FormatMarkdown(IBufferWriter<byte> snapshot, ISyntaxNode value)
    {
        snapshot.Append("```graphql");
        snapshot.AppendLine();
        Format(snapshot, value);
        snapshot.AppendLine();
        snapshot.Append("```");
        snapshot.AppendLine();
    }
}
