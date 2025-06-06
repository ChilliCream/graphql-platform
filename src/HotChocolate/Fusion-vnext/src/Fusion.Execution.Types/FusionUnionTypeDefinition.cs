using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;
using static HotChocolate.Fusion.Types.ThrowHelper;

namespace HotChocolate.Fusion.Types;

public sealed class FusionUnionTypeDefinition : IUnionTypeDefinition
{
    private bool _completed;

    public FusionUnionTypeDefinition(string name, string? description)
    {
        Name = name;
        Description = description;

        // these properties are initialized
        // in the type complete step.
        Types = null!;
        Directives = null!;
        Features = null!;
    }

    public string Name { get; }

    public string? Description { get; }

    public TypeKind Kind => TypeKind.Union;

    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    public FusionObjectTypeDefinitionCollection Types
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    IReadOnlyObjectTypeDefinitionCollection IUnionTypeDefinition.Types => Types;

    public FusionDirectiveCollection Directives
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => Directives;

    public IFeatureCollection Features
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    internal void Complete(CompositeUnionTypeCompletionContext context)
    {
        EnsureNotSealed(_completed);

        Directives = context.Directives;
        Types = context.Types;
        Features = context.Features;

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

        return other is FusionUnionTypeDefinition otherUnion && otherUnion.Name.Equals(Name, StringComparison.Ordinal);
    }

    public bool IsAssignableFrom(ITypeDefinition type)
    {
        switch (type.Kind)
        {
            case TypeKind.Union:
                return ReferenceEquals(type, this);

            case TypeKind.Object:
                return Types.ContainsName(((FusionObjectTypeDefinition)type).Name);

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
