using System.Buffers;
using HotChocolate;

namespace CookieCrumble.Formatters;

internal sealed class SchemaSnapshotValueFormatter : SnapshotValueFormatter<ISchema>
{
    protected override void Format(IBufferWriter<byte> snapshot, ISchema value)
        => snapshot.Append(value.ToString());

    protected override void FormatMarkdown(IBufferWriter<byte> snapshot, ISchema value)
    {
        snapshot.Append("```graphql");
        snapshot.AppendLine();
        Format(snapshot, value);
        snapshot.AppendLine();
        snapshot.Append("```");
        snapshot.AppendLine();
    }
}
