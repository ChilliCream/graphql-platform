using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Fusion.Types.ThrowHelper;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a GraphQL union type definition in a fusion schema.
/// </summary>
public sealed class FusionUnionTypeDefinition : IUnionTypeDefinition, IFusionTypeDefinition
{
    private bool _completed;

    /// <summary>
    /// Initializes a new instance of <see cref="FusionUnionTypeDefinition"/>.
    /// </summary>
    /// <param name="name">The name of the union type.</param>
    /// <param name="description">The description of the union type.</param>
    /// <param name="isInaccessible">A value indicating whether the union type is marked as inaccessible.</param>
    public FusionUnionTypeDefinition(string name, string? description, bool isInaccessible)
    {
        Name = name.EnsureGraphQLName();
        Description = description;
        IsInaccessible = isInaccessible;

        // these properties are initialized
        // in the type complete step.
        Types = null!;
        Directives = null!;
        Features = null!;
    }

    /// <summary>
    /// Gets the name of this union type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of this union type.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the kind of this type.
    /// </summary>
    public TypeKind Kind => TypeKind.Union;

    /// <summary>
    /// Gets the schema coordinate of this union type.
    /// </summary>
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    Type IRuntimeTypeProvider.RuntimeType => typeof(object);

    /// <summary>
    /// Gets a value indicating whether this union type is marked as inaccessible.
    /// </summary>
    public bool IsInaccessible { get; }

    /// <summary>
    /// Gets metadata about this union type in its source schemas.
    /// Each entry in the collection provides information about this union type
    /// that is specific to the source schemas the type was composed of.
    /// </summary>
    public SourceUnionTypeCollection Sources
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);

            field = value;
        }
    } = null!;

    /// <summary>
    /// Gets the collection of object types that are members of this union.
    /// </summary>
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

    /// <summary>
    /// Gets the directives applied to this union type.
    /// </summary>
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

    /// <summary>
    /// Gets the feature collection associated with this union type.
    /// </summary>
    public IFeatureCollection Features
    {
        get;
        private set
        {
            EnsureNotSealed(_completed);
            field = value;
        }
    }

    /// <summary>
    /// Completes the initialization of this union type by setting properties
    /// that are populated during the schema completion phase.
    /// </summary>
    /// <param name="context">The completion context containing required properties.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the context has an invalid state (required properties are null).
    /// </exception>
    internal void Complete(CompositeUnionTypeCompletionContext context)
    {
        EnsureNotSealed(_completed);

        if (context.Directives is null || context.Types is null
            || context.Sources is null || context.Features is null)
        {
            throw InvalidCompletionContext();
        }

        Directives = context.Directives;
        Types = context.Types;
        Sources = context.Sources;
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

    /// <inheritdoc />
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
    /// Gets the string representation of this union type definition.
    /// </summary>
    /// <returns>
    /// The string representation of this union type definition.
    /// </returns>
    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="UnionTypeDefinitionNode"/> from this
    /// <see cref="FusionUnionTypeDefinition"/>.
    /// </summary>
    /// <returns>
    /// A <see cref="UnionTypeDefinitionNode"/> representing this union type.
    /// </returns>
    public UnionTypeDefinitionNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);
}
