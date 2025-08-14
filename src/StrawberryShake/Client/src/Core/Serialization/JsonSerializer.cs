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
        return serializedValue.Clone();
    }

    protected override JsonElement Format(JsonElement runtimeValue)
        => runtimeValue;
}
