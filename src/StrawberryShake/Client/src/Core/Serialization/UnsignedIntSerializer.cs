namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles unsigned int scalars.
/// </summary>
public class UnsignedIntSerializer : ScalarSerializer<uint>
{
    public UnsignedIntSerializer(string typeName = BuiltInScalarNames.UnsignedInt)
        : base(typeName)
    {
    }
}
