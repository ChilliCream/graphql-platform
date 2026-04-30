namespace HotChocolate.Features;

/// <summary>
/// Marks a type definition as a type extension. The presence of this feature on a
/// type definition indicates that the canonical SDL formatter should emit the type
/// using the corresponding <c>extend</c> syntax (for example, <c>extend type Foo</c>).
/// </summary>
public sealed class TypeExtensionMarker
{
    /// <summary>
    /// Gets the singleton marker instance.
    /// </summary>
    public static readonly TypeExtensionMarker Instance = new();

    private TypeExtensionMarker()
    {
    }
}
