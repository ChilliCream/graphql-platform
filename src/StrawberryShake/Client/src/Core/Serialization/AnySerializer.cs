using System.Text.Json;

namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles <c>Any</c> scalars.
/// </summary>
public class AnySerializer : ScalarSerializer<JsonElement, JsonElement>
{
    public AnySerializer(string typeName = BuiltInScalarNames.Any)
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
