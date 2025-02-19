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
    , IFeatureProvider
{
    private bool _isDeprecated;
    private DirectiveCollection? _directives;

    /// <summary>
    /// Represents a GraphQL enum value.
    /// </summary>
    public MutableEnumValue(string name)
    {
        Name = name;
    }

    /// <inheritdoc cref="INameProvider.Name" />
    public string Name
    {
        get;
        set => field = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="IDescriptionProvider.Description" />
    public string? Description { get; set; }

    /// <inheritdoc cref="IDeprecationProvider.IsDeprecated" />
    public bool IsDeprecated
    {
        get => _isDeprecated;
        set
        {
            _isDeprecated = value;

            if (!value)
            {
                DeprecationReason = null;
            }
        }
    }

    /// <inheritdoc cref="IDeprecationProvider.DeprecationReason" />
    public string? DeprecationReason
    {
        get;
        set
        {
            field = value;

            if (!string.IsNullOrEmpty(value))
            {
                _isDeprecated = true;
            }
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
