using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL enum value.
/// </summary>
public class MutableEnumValue
    : INamedTypeSystemMemberDefinition<MutableEnumValue>
        , IEnumValue
{
    private DirectiveCollection? _directives;

    /// <summary>
    /// Represents a GraphQL enum value.
    /// </summary>
    public MutableEnumValue(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the declaring type of the enum value.
    /// </summary>
    public MutableEnumTypeDefinition? DeclaringType { get; set; }

    IEnumTypeDefinition IEnumValue.DeclaringType
        => DeclaringType ?? throw new InvalidOperationException(
            "The declaring type of the enum value is not set.");

    /// <inheritdoc cref="INameProvider.Name" />
    public string Name
    {
        get;
        set => field = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="IDescriptionProvider.Description" />
    public string? Description { get; set; }

    /// <inheritdoc cref="IDeprecationProvider.IsDeprecated" />
    [MemberNotNullWhen(true, nameof(DeprecationReason))]
    public bool IsDeprecated => DeprecationReason is not null;

    /// <summary>
    /// Gets or sets the deprecation reason of this enum value, or <c>null</c> if this enum value
    /// is not deprecated. Setting an empty or white-space value is equivalent to setting <c>null</c>.
    /// </summary>
    public string? DeprecationReason
    {
        get;
        set => field = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public SchemaCoordinate Coordinate
    {
        get
        {
            if (DeclaringType is null)
            {
                throw new InvalidOperationException(
                    "The declaring type of the enum value is not set.");
            }

            return new SchemaCoordinate(DeclaringType.Name, Name, ofDirective: false);
        }
    }

    public DirectiveCollection Directives
        => _directives ??= [];

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives as IReadOnlyDirectiveCollection ?? EmptyCollections.Directives;

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

    /// <summary>
    /// Gets a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString()
        => Format(this).ToString(true);

    /// <summary>
    /// Creates an <see cref="EnumValueDefinitionNode"/> from an <see cref="MutableEnumValue"/>.
    /// </summary>
    public EnumValueDefinitionNode ToSyntaxNode() => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => Format(this);

    /// <summary>
    /// Creates a new enum value.
    /// </summary>
    /// <param name="name">
    /// The name of the enum value.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="MutableEnumValue"/>.
    /// </returns>
    public static MutableEnumValue Create(string name) => new(name);
}
