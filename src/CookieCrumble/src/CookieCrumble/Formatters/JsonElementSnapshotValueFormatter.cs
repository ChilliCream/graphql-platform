using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CookieCrumble.Formatters;

internal sealed class JsonElementSnapshotValueFormatter() : SnapshotValueFormatter<JsonElement>("json")
{
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    protected override void Format(IBufferWriter<byte> snapshot, JsonElement value)
    {
        var buffer = JsonSerializer.SerializeToUtf8Bytes(value, _options);
        var span = snapshot.GetSpan(buffer.Length);
        buffer.AsSpan().CopyTo(span);
        snapshot.Advance(buffer.Length);
    }
}
