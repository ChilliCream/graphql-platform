using HotChocolate.Language;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// This type definition is used when building fields
/// or arguments where the type cannot be yet set.
/// </summary>
public sealed class NotSetTypeDefinition : ITypeDefinition
{
    private NotSetTypeDefinition()
    {
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Scalar;

    public override string ToString()
        => "__NotSet";

    public ISyntaxNode ToSyntaxNode()
        => new NamedTypeNode("__NotSet");

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other)
        => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is NotSetTypeDefinition;
    }

    /// <summary>
    /// Gets the default instance of <see cref="NotSetTypeDefinition"/>.
    /// </summary>
    /// <returns>
    /// Returns the default instance of <see cref="NotSetTypeDefinition"/>.
    /// </returns>
    public static readonly NotSetTypeDefinition Default = new();
}
