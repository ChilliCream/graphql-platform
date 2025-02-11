using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL enum type definition.
/// </summary>
public sealed class MutableEnumTypeDefinition : INamedTypeSystemMemberDefinition<MutableEnumTypeDefinition>
    , IEnumTypeDefinition
    , IFeatureProvider
{
    private readonly EnumValueCollection _values = [];
    private DirectiveCollection? _directives;

    /// <summary>
    /// Represents a GraphQL enum type definition.
    /// </summary>
    public MutableEnumTypeDefinition(string name)
    {
        Name = name;
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Enum;

    /// <inheritdoc cref="INamedTypeDefinition.Name" />
    public string Name
    {
        get;
        set => field = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="INamedTypeDefinition.Description" />
    public string? Description { get; set; }

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
    public EnumValueCollection Values
        => _values;

    IReadOnlyEnumValueCollection IEnumTypeDefinition.Values
        => _values;

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

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
    public bool Equals(ITypeDefinition? other) => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is MutableEnumTypeDefinition otherEnum && otherEnum.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool IsAssignableFrom(INamedTypeDefinition type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

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
