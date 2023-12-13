namespace HotChocolate.Fusion.Metadata;

[Flags]
public enum ObjectFieldFlags : byte
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// This field represents the __typename field.
    /// </summary>
    TypeName = 2
}
