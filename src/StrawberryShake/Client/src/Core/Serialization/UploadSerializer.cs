namespace StrawberryShake.Serialization;

/// <summary>
/// This serializer handles upload scalars.
/// </summary>
public class UploadSerializer : ScalarSerializer<string, Upload>
{
    /// <summary>
    /// Creates a new instance of <see cref="UploadSerializer"/>
    /// </summary>
    /// <param name="typeName"></param>
    public UploadSerializer(string typeName = BuiltInScalarNames.Upload)
        : base(typeName)
    {
    }

    public override Upload Parse(string serializedValue)
        => throw ThrowHelper.UploadScalar_OutputNotSupported();

    protected override string Format(Upload runtimeValue) => null!;
}
