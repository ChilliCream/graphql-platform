namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles unsigned byte scalars.
/// </summary>
public class UnsignedByteSerializer : ScalarSerializer<byte>
{
    public UnsignedByteSerializer(string typeName = BuiltInScalarNames.UnsignedByte)
        : base(typeName)
    {
    }
}
