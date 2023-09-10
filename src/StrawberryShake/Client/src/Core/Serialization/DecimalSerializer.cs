namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles decimal scalars.
/// </summary>
public class DecimalSerializer : ScalarSerializer<decimal>
{
    public DecimalSerializer(string typeName = BuiltInScalarNames.Decimal)
        : base(typeName)
    {
    }
}
