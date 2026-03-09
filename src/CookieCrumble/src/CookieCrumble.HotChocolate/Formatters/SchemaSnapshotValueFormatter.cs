using System.Buffers;
using CookieCrumble.Formatters;
using HotChocolate;

namespace CookieCrumble.HotChocolate.Formatters;

internal sealed class SchemaSnapshotValueFormatter() : SnapshotValueFormatter<ISchemaDefinition>("graphql")
{
    protected override void Format(IBufferWriter<byte> snapshot, ISchemaDefinition value)
        => snapshot.Append(value.ToString());
}
