namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles unsigned short scalars.
/// </summary>
public class UnsignedShortSerializer : ScalarSerializer<ushort>
{
    public UnsignedShortSerializer(string typeName = BuiltInScalarNames.UnsignedShort)
        : base(typeName)
    {
    }
}
