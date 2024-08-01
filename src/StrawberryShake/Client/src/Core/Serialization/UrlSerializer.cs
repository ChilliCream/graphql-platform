namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles url scalars.
/// </summary>
public class UrlSerializer : ScalarSerializer<string, Uri>
{
    public UrlSerializer(string typeName = BuiltInScalarNames.Url)
        : base(typeName)
    {
    }

    public override Uri Parse(string serializedValue)
    {
        if (!Uri.TryCreate(serializedValue, UriKind.RelativeOrAbsolute, out var uri))
        {
            throw ThrowHelper.UrlFormatter_CouldNotParseUri(serializedValue);
        }

        return uri;
    }

    protected override string Format(Uri runtimeValue) => runtimeValue.ToString();
}
