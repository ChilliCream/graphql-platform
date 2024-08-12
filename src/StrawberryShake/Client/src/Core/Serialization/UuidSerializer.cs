namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles UUID scalars.
/// </summary>
public class UUIDSerializer : ScalarSerializer<string, Guid>
{
    private readonly string _format;

    public UUIDSerializer(string typeName = BuiltInScalarNames.UUID, string format = "D")
        : base(typeName)
    {
        _format = format;
    }

    public override Guid Parse(string serializedValue)
    {
        if (Guid.TryParse(serializedValue, out var guid))
        {
            return guid;
        }
        throw ThrowHelper.UuidSerializer_CouldNotParse(serializedValue);
    }

    protected override string Format(Guid runtimeValue)
    {
        return runtimeValue.ToString(_format);
    }
}
