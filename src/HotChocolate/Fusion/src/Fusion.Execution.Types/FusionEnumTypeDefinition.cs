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
/// Represents a GraphQL enum type definition in a fusion schema.
/// </summary>
public sealed class FusionEnumTypeDefinition : IEnumTypeDefinition, IFusionTypeDefinition
{
    private bool _completed;

    /// <summary>
    /// Initializes a new instance of <see cref="FusionEnumTypeDefinition"/>.
    /// </summary>
    /// <param name="name">The name of the enum type.</param>
    /// <param name="description">The description of the enum type.</param>
    /// <param name="isInaccessible">A value indicating whether the enum type is marked as inaccessible.</param>
    /// <param name="values">The collection of enum values.</param>
    public FusionEnumTypeDefinition(
        string name,
        string? description,
        bool isInaccessible,
        FusionEnumValueCollection values)
    {
        name.EnsureGraphQLName();
        ArgumentNullException.ThrowIfNull(values);

        Name = name;
        Description = description;
        IsInaccessible = isInaccessible;
        Values = values;

        // these properties are initialized
        // in the type complete step.
        Directives = null!;
        Features = null!;
    }

    /// <summary>
    /// Gets the name of this enum type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of this enum type.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the kind of this type.
    /// </summary>
    public TypeKind Kind => TypeKind.Enum;

    /// <summary>
    /// Gets the schema coordinate of this enum type.
    /// </summary>
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    Type IRuntimeTypeProvider.RuntimeType => typeof(object);

    /// <summary>
    /// Gets a value indicating whether this enum type is marked as inaccessible.
    /// </summary>
    public bool IsInaccessible { get; }

    /// <summary>
    /// Gets the collection of enum values for this enum type.
    /// </summary>
    public FusionEnumValueCollection Values { get; }

    IReadOnlyEnumValueCollection IEnumTypeDefinition.Values => Values;

    /// <summary>
    /// Gets the directives applied to this enum type.
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
    /// Gets the feature collection associated with this enum type.
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

    internal void Complete(CompositeEnumTypeCompletionContext context)
    {
        EnsureNotSealed(_completed);

        if (context.Directives is null || context.Features is null)
        {
            throw InvalidCompletionContext();
        }

        Directives = context.Directives;
        Features = context.Features;

        _completed = true;
    }

    /// <summary>
    /// Gets the string representation of this enum type definition.
    /// </summary>
    /// <returns>
    /// The string representation of this enum type definition.
    /// </returns>
    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="EnumTypeDefinitionNode"/> from a
    /// <see cref="FusionEnumTypeDefinition"/>.
    /// </summary>
    public EnumTypeDefinitionNode ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode()
        => SchemaDebugFormatter.Format(this);

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
