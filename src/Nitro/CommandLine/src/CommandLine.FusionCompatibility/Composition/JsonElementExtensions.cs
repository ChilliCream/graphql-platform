using System.Buffers;
using System.Text.Json;

namespace ChilliCream.Nitro.CommandLine.FusionCompatibility;

internal static class JsonElementExtensions
{
    public static JsonElement SafeClone(this JsonElement element)
    {
        var writer = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(writer);

        element.WriteTo(jsonWriter);
        jsonWriter.Flush();

        var reader = new Utf8JsonReader(writer.WrittenSpan, true, default);
        return JsonElement.ParseValue(ref reader);
    }
}
