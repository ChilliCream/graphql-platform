using System.Text.Json;
using StrawberryShake.Internal;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles json scalars.
/// </summary>
public class JsonSerializer : ScalarSerializer<JsonElement, JsonElement>
{
    public JsonSerializer(string typeName = BuiltInScalarNames.Any)
        : base(typeName)
    {
    }

    public override JsonElement Parse(JsonElement serializedValue)
    {
        using var writer = new ArrayWriter();

        // write json value to buffer.
        using var jsonWriter = new Utf8JsonWriter(writer);
        serializedValue.WriteTo(jsonWriter);
        jsonWriter.Flush();

        // now we read the buffer and create an element that does not need to be disposed.
        var reader = new Utf8JsonReader(writer.GetWrittenSpan(), true, default);
        return JsonElement.ParseValue(ref reader);
    }

    protected override JsonElement Format(JsonElement runtimeValue)
        => runtimeValue;
}
