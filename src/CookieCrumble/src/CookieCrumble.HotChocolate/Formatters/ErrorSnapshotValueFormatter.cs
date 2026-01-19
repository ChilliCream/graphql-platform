using System.Buffers;
using System.Text.Json;
using CookieCrumble.Formatters;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Text.Json;

namespace CookieCrumble.HotChocolate.Formatters;

internal sealed class ErrorSnapshotValueFormatter()
    : SnapshotValueFormatter<IError>("json")
{
    protected override void Format(IBufferWriter<byte> snapshot, IError value)
    {
        var jsonWriter = new JsonWriter(snapshot, new JsonWriterOptions { Indented = true });
        JsonValueFormatter.WriteError(jsonWriter, value, new JsonSerializerOptions { WriteIndented = true }, default);
    }
}
