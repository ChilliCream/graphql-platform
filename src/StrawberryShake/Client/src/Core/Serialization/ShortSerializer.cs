namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles short scalars.
/// </summary>
public class ShortSerializer : ScalarSerializer<short>
{
    public ShortSerializer(string typeName = BuiltInScalarNames.Short)
        : base(typeName)
    {
    }
}
