using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionUnionTypeDefinition(
    string name,
    string? description)
    : IUnionTypeDefinition
{
    private FusionObjectTypeDefinitionCollection _types = default!;
    private FusionDirectiveCollection _directives = default!;
    private bool _completed;

    public string Name { get; } = name;

    public string? Description { get; } = description;

    public FusionDirectiveCollection Directives => _directives;

    public TypeKind Kind => TypeKind.Union;

    public FusionObjectTypeDefinitionCollection Types => _types;

    IReadOnlyObjectTypeDefinitionCollection IUnionTypeDefinition.Types => _types;

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives;

    internal void Complete(CompositeUnionTypeCompletionContext context)
    {
        if (_completed)
        {
            throw new NotSupportedException(
                "The type definition is sealed and cannot be modified.");
        }

        _directives = new FusionDirectiveCollection(context.Directives);
        _types = new FusionObjectTypeDefinitionCollection(context.Types);
        _completed = true;
    }

    /// <inheritdoc />
    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is FusionUnionTypeDefinition otherUnion
            && otherUnion.Name.Equals(Name, StringComparison.Ordinal);
    }

    public bool IsAssignableFrom(ITypeDefinition type)
    {
        switch (type.Kind)
        {
            case TypeKind.Union:
                return ReferenceEquals(type, this);

            case TypeKind.Object:
                return _types.ContainsName(((FusionObjectTypeDefinition)type).Name);

            default:
                return false;
        }
    }

     /// <summary>
    /// Get the string representation of the union type definition.
    /// </summary>
    /// <returns>
    /// Returns the string representation of the union type definition.
    /// </returns>
    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="UnionTypeDefinitionNode"/>
    /// from a <see cref="FusionUnionTypeDefinition"/>.
    /// </summary>
    public UnionTypeDefinitionNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);
}
