using System.Text.Json;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion;

internal static class JsonElementExtensions
{
    public static JsonElement SafeClone(this JsonElement element)
    {
        using var writer = new ArrayWriter();
        using var jsonWriter = new Utf8JsonWriter(writer);

        element.WriteTo(jsonWriter);
        jsonWriter.Flush();
        var reader = new Utf8JsonReader(writer.GetWrittenSpan(), true, default);

        return JsonElement.ParseValue(ref reader);
    }
}
