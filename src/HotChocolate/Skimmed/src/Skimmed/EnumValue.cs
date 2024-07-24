using HotChocolate.Features;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL enum value.
/// </summary>
public sealed class EnumValue(string name)
    : INamedTypeSystemMemberDefinition<EnumValue>
    , IDirectivesProvider
    , IFeatureProvider
    , IDescriptionProvider
    , IDeprecationProvider
    , ISealable
{
    private string _name = name.EnsureGraphQLName();
    private string? _description;
    private bool _isDeprecated;
    private string? _deprecationReason;
    private IDirectiveCollection? _directives;
    private IFeatureCollection? _features;
    private bool _isReadOnly;

    /// <inheritdoc />
    public string Name
    {
        get => _name;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The value is sealed and cannot be modified.");
            }

            _name = value.EnsureGraphQLName();
        }
    }

    /// <inheritdoc />
    public string? Description
    {
        get => _description;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The value is sealed and cannot be modified.");
            }

            _description = value;
        }
    }

    /// <inheritdoc />
    public bool IsDeprecated
    {
        get => _isDeprecated;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The value is sealed and cannot be modified.");
            }

            _isDeprecated = value;

            if (!value)
            {
                DeprecationReason = null;
            }
        }
    }

    /// <inheritdoc />
    public string? DeprecationReason
    {
        get => _deprecationReason;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The value is sealed and cannot be modified.");
            }

            _deprecationReason = value;

            if (!string.IsNullOrEmpty(value))
            {
                _isDeprecated = true;
            }
        }
    }

    /// <inheritdoc />
    public IDirectiveCollection Directives
        => _directives ??= new DirectiveCollection();

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

    /// <inheritdoc />
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Seals this value and makes it read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    internal void Seal()
    {
        if (_isReadOnly)
        {
            return;
        }

        _directives = _directives is null
            ? ReadOnlyDirectiveCollection.Empty
            : ReadOnlyDirectiveCollection.From(_directives);

        _features = _features is null
            ? EmptyFeatureCollection.Default
            : _features.ToReadOnly();

        _isReadOnly = true;
    }

    void ISealable.Seal() => Seal();

    /// <summary>
    /// Gets a string that represents the current object.
    /// </summary>
    /// <returns>
    /// A string that represents the current object.
    /// </returns>
    public override string ToString()
        => RewriteEnumValue(this).ToString(true);

    /// <summary>
    /// Creates a new enum value.
    /// </summary>
    /// <param name="name">
    /// The name of the enum value.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="EnumValue"/>.
    /// </returns>
    public static EnumValue Create(string name) => new(name);
}
