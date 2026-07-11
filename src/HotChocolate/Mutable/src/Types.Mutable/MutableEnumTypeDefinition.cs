using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL enum type definition.
/// </summary>
public class MutableEnumTypeDefinition
    : INamedTypeSystemMemberDefinition<MutableEnumTypeDefinition>
    , IEnumTypeDefinition
    , IMutableTypeDefinition
    , IFeatureProvider
{
    private DirectiveCollection? _directives;

    /// <summary>
    /// Represents a GraphQL enum type definition.
    /// </summary>
    public MutableEnumTypeDefinition(string name)
    {
        Name = name;
        Values = new EnumValueCollection(this);
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Enum;

    /// <inheritdoc cref="IMutableTypeDefinition.Name" />
    public string Name
    {
        get;
        set => field = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="IMutableTypeDefinition.Description" />
    public string? Description { get; set; }

    Type IRuntimeTypeProvider.RuntimeType => typeof(object);

    public DirectiveCollection Directives
        => _directives ??= [];

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives ?? EmptyCollections.Directives;

    /// <summary>
    /// Gets the values of this enum type.
    /// </summary>
    /// <value>
    /// The values of this enum type.
    /// </value>
    public EnumValueCollection Values { get; }

    IReadOnlyEnumValueCollection IEnumTypeDefinition.Values
        => Values;

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

    public SchemaCoordinate Coordinate
        => new(Name, ofDirective: false);

    /// <inheritdoc cref="IMutableTypeDefinition.IsIntrospectionType" />
    public bool IsIntrospectionType { get; set; }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => Format(this).ToString(true);

    /// <summary>
    /// Creates an <see cref="EnumTypeDefinitionNode"/> from an <see cref="MutableEnumTypeDefinition"/>.
    /// </summary>
    public EnumTypeDefinitionNode ToSyntaxNode() => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => Format(this);

    /// <inheritdoc />
    public bool Equals(IType? other) => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(IType? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is MutableEnumTypeDefinition otherEnum
            && otherEnum.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool IsAssignableFrom(ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.Kind == TypeKind.Enum)
        {
            return Equals(type, TypeComparison.Reference);
        }

        return false;
    }

    /// <summary>
    /// Creates a new instance of <see cref="MutableEnumTypeDefinition"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the enum type.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="MutableEnumTypeDefinition"/>.
    /// </returns>
    public static MutableEnumTypeDefinition Create(string name) => new(name);
}
