namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles int scalars.
/// </summary>
public class IntSerializer : ScalarSerializer<int>
{
    public IntSerializer(string typeName = BuiltInScalarNames.Int)
        : base(typeName)
    {
    }
}
