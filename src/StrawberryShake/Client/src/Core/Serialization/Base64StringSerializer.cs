namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles base64 string scalars.
/// </summary>
public class Base64StringSerializer : ScalarSerializer<byte[]>
{
    public Base64StringSerializer(string typeName = BuiltInScalarNames.Base64String)
        : base(typeName)
    {
    }
}
