namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles boolean scalars.
/// </summary>
public class BooleanSerializer : ScalarSerializer<bool>
{
    public BooleanSerializer(string typeName = BuiltInScalarNames.Boolean)
        : base(typeName)
    {
    }
}
