#nullable enable

using HotChocolate.Types;

namespace HotChocolate.Internal;

/// <summary>
/// The type attribute interface is implement by GraphQL type attributes and provides
/// additional information about the annotated type.
/// </summary>
public interface ITypeAttribute
{
    /// <summary>
    /// Defines if this attribute is inherited. The default is <c>false</c>.
    /// </summary>
    public bool Inherited { get; set; }

    /// <summary>
    /// Gets the kind of type represented by this attribute.
    /// </summary>
    public TypeKind Kind { get; }

    /// <summary>
    /// Defines if this attribute represents a type extension.
    /// </summary>
    public bool IsTypeExtension { get; }
}
