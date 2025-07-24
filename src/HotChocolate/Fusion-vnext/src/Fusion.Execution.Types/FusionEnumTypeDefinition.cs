using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;
using static HotChocolate.Fusion.Types.ThrowHelper;

namespace HotChocolate.Fusion.Types;

public sealed class FusionEnumTypeDefinition : IEnumTypeDefinition
{
    private bool _completed;

    public FusionEnumTypeDefinition(
        string name,
        string? description,
        FusionEnumValueCollection values)
    {
        Name = name;
        Description = description;
        Values = values;

        // these properties are initialized
        // in the type complete step.
        Directives = null!;
        Features = null!;
    }

    public string Name { get; }

    public string? Description { get; }

    public TypeKind Kind => TypeKind.Enum;

    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    public FusionEnumValueCollection Values { get; }

    IReadOnlyEnumValueCollection IEnumTypeDefinition.Values => Values;

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

    internal void Complete(CompositeEnumTypeCompletionContext context)
    {
        EnsureNotSealed(_completed);

        Directives = context.Directives;
        Features = context.Features;

        _completed = true;
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
    public EnumTypeDefinitionNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    /// <inheritdoc />
    public bool Equals(IType? other)
        => Equals(other, TypeComparison.Reference);

    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is FusionEnumTypeDefinition otherEnum
            && otherEnum.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool IsAssignableFrom(ITypeDefinition type)
    {
        if (type.Kind == TypeKind.Enum)
        {
            return Equals(type, TypeComparison.Reference);
        }

        return false;
    }
}
