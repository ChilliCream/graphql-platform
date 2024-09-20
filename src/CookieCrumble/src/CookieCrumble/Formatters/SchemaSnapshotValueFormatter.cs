using System.Buffers;
using HotChocolate;

namespace CookieCrumble.Formatters;

internal sealed class SchemaSnapshotValueFormatter() : SnapshotValueFormatter<ISchema>("graphql")
{
    protected override void Format(IBufferWriter<byte> snapshot, ISchema value)
        => snapshot.Append(value.ToString());
}
