using HotChocolate.Language;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types;

/// <summary>
/// Represents a GraphQL list type.
/// </summary>
public sealed class ListType : IWrapperType
{
    /// <summary>
    /// Represents a GraphQL list type definition.
    /// </summary>
    public ListType(IType elementType)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ElementType = elementType;
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.List;

    /// <summary>
    /// Gets the element type of the list.
    /// </summary>
    public IType ElementType { get; }

    IType IWrapperType.InnerType => ElementType;

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => FormatTypeRef(this).ToString(true);

    public ISyntaxNode ToSyntaxNode()
        => FormatTypeRef(this);

    /// <inheritdoc />
    public bool Equals(IType? other)
        => this.Equals(other, TypeComparison.Reference);
}
