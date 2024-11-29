using System.Buffers;
using CookieCrumble.Formatters;
using HotChocolate;

namespace CookieCrumble.HotChocolate.Formatters;

internal sealed class SchemaSnapshotValueFormatter() : SnapshotValueFormatter<ISchema>("graphql")
{
    protected override void Format(IBufferWriter<byte> snapshot, ISchema value)
        => snapshot.Append(value.ToString());
}
