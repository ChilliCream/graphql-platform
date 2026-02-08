using System.Buffers;
using CookieCrumble.Formatters;
using HotChocolate.Text.Json;

namespace CookieCrumble.HotChocolate.Formatters;

internal sealed class ResultElementSnapshotValueFormatter
    : SnapshotValueFormatter<ResultElement>
{
    protected override void Format(IBufferWriter<byte> snapshot, ResultElement element)
    => element.WriteTo(snapshot, indented: true);
}
