using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL enum type definition.
/// </summary>
public class EnumTypeDefinition(string name)
    : INamedTypeDefinition
    , INamedTypeSystemMemberDefinition<EnumTypeDefinition>
    , ISealable
{
    private string _name = name.EnsureGraphQLName();
    private string? _description;
    private IDirectiveCollection? _directives;
    private IEnumValueCollection _values = new EnumValueCollection();
    private IFeatureCollection? _features;
    private bool _isReadOnly;

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Enum;

    /// <inheritdoc cref="INamedTypeDefinition.Name" />
    public string Name
    {
        get => _name;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The type is sealed and cannot be modified.");
            }

            _name = value.EnsureGraphQLName();
        }
    }

    /// <inheritdoc cref="INamedTypeDefinition.Description" />
    public string? Description
    {
        get => _description;
        set
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException(
                    "The type is sealed and cannot be modified.");
            }

            _description = value;
        }
    }

    /// <inheritdoc />
    public IDirectiveCollection Directives
        => _directives ??= new DirectiveCollection();

    /// <summary>
    /// Gets the values of this enum type.
    /// </summary>
    /// <value>
    /// The values of this enum type.
    /// </value>
    public IEnumValueCollection Values
        => _values;

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

    /// <inheritdoc />
    public bool IsReadOnly => _isReadOnly;

    /// <summary>
    /// Seals this type and makes it read-only.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    protected internal void Seal()
    {
        if (_isReadOnly)
        {
            return;
        }

        if(_values.Count == 0)
        {
            throw new InvalidOperationException(
                "An enum type must have at least one value.");
        }

        _directives = _directives is null
            ? ReadOnlyDirectiveCollection.Empty
            : ReadOnlyDirectiveCollection.From(_directives);

        _values = ReadOnlyEnumValueCollection.From(_values);

        _features = _features is null
            ? EmptyFeatureCollection.Default
            : _features.ToReadOnly();

        foreach (var value in _values)
        {
            value.Seal();
        }

        _isReadOnly = true;
    }

    void ISealable.Seal() => Seal();

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => RewriteEnumType(this).ToString(true);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other) => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is EnumTypeDefinition otherEnum && otherEnum.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a new instance of <see cref="EnumTypeDefinition"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the enum type.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="EnumTypeDefinition"/>.
    /// </returns>
    public static EnumTypeDefinition Create(string name) => new(name);
}
