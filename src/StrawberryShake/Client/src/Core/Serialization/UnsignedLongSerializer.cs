namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles unsigned long scalars.
/// </summary>
public class UnsignedLongSerializer : ScalarSerializer<ulong>
{
    public UnsignedLongSerializer(string typeName = BuiltInScalarNames.UnsignedLong)
        : base(typeName)
    {
    }
}
