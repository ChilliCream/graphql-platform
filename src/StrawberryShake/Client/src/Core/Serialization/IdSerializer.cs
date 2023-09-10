namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles ID scalars.
/// </summary>
public class IdSerializer : ScalarSerializer<string>
{
    public IdSerializer(string typeName = BuiltInScalarNames.ID)
        : base(typeName)
    {
    }
}
