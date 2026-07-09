namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles URI scalars.
/// </summary>
public class UriSerializer : ScalarSerializer<string, Uri>
{
    public UriSerializer(string typeName = BuiltInScalarNames.URI)
        : base(typeName)
    {
    }

    public override Uri Parse(string serializedValue)
    {
        if (!Uri.TryCreate(serializedValue, UriKind.RelativeOrAbsolute, out var uri))
        {
            throw ThrowHelper.UriFormatter_CouldNotParseUri(serializedValue);
        }

        return uri;
    }

    protected override string Format(Uri runtimeValue) => runtimeValue.ToString();
}
