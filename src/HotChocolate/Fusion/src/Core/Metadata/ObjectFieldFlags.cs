namespace HotChocolate.Fusion.Metadata;

[Flags]
public enum ObjectFieldFlags : byte
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Specifies that the field value represents an encoded identifier that needs to be re-encoded.
    /// </summary>
    ReEncodeId = 1,

    /// <summary>
    /// This field represents the __typename field.
    /// </summary>
    TypeName = 2,
}
