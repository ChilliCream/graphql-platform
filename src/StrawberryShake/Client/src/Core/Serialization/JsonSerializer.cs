using System.Text.Json;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles json scalars.
/// </summary>
public class JsonSerializer : ScalarSerializer<JsonElement, JsonDocument>
{
    public JsonSerializer(string typeName = BuiltInScalarNames.Any)
        : base(typeName)
    {
    }

    public override JsonDocument Parse(JsonElement serializedValue)
    {
        return JsonDocument.Parse(serializedValue.GetRawText());
    }

    protected override JsonElement Format(JsonDocument runtimeValue)
    {
        return runtimeValue.RootElement;
    }
}
