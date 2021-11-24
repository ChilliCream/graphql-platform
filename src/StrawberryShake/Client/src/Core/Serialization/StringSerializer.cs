namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles string scalars.
/// </summary>
public class StringSerializer : ScalarSerializer<string>
{
    public StringSerializer(string typeName = BuiltInScalarNames.String)
        : base(typeName)
    {
    }
}
