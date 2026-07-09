using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using CookieCrumble.Formatters;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Text.Json;

namespace CookieCrumble.HotChocolate.Formatters;

internal sealed class ErrorListSnapshotValueFormatter
    : SnapshotValueFormatter<IReadOnlyList<IError>>
{
    protected override void Format(IBufferWriter<byte> snapshot, IReadOnlyList<IError> value)
    {
        var writerOptions = new JsonWriterOptions
        {
            Indented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var serializationOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var writer = new JsonWriter(snapshot, writerOptions);
        JsonValueFormatter.WriteErrors(writer, value, serializationOptions);
    }
}
