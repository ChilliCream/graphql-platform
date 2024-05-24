#if NET7_0_OR_GREATER
using System.Buffers;
using HotChocolate.Skimmed;
using HotChocolate.Skimmed.Serialization;

namespace CookieCrumble.Formatters;

internal sealed class SkimmedSchemaSnapshotValueFormatter() : SnapshotValueFormatter<Schema>("graphql")
{
    protected override void Format(IBufferWriter<byte> snapshot, Schema value)
        => snapshot.Append(SchemaFormatter.FormatAsString(value));
}
#endif
