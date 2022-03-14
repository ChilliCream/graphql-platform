namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles float scalars.
/// </summary>
public class FloatSerializer : ScalarSerializer<double>
{
    public FloatSerializer(string typeName = BuiltInScalarNames.Float)
        : base(typeName)
    {
    }
}
