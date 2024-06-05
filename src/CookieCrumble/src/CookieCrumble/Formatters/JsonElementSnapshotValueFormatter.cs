using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CookieCrumble.Formatters;

internal sealed class JsonElementSnapshotValueFormatter() : SnapshotValueFormatter<JsonElement>("json")
{
    private readonly JsonSerializerOptions _options =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

    protected override void Format(IBufferWriter<byte> snapshot, JsonElement value)
    {
        var buffer = JsonSerializer.SerializeToUtf8Bytes(value, _options);
        var span = snapshot.GetSpan(buffer.Length);
        buffer.AsSpan().CopyTo(span);
        snapshot.Advance(buffer.Length);
    }
}
