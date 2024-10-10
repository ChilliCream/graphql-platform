using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a GraphQL directive type definition.
/// </summary>
public class DirectiveTypeDefinition(string name)
    : INamedTypeDefinition
        , INamedTypeSystemMemberDefinition<DirectiveTypeDefinition>
        , ISealable
{
    private string _name = name.EnsureGraphQLName();
    private IDirectiveCollection? _directives;
    private IFeatureCollection? _features;
    private string? _description;
    private bool _isReadOnly;

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Directive;

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

    /// <inheritdoc />
    public IFeatureCollection Features
        => _features ??= new FeatureCollection();

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
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        // TODO: Implement
        => Name;
        // => RewriteScalarType(this).ToString(true);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other)
        => Equals(other, TypeComparison.Reference);

    /// <inheritdoc />
    public bool Equals(ITypeDefinition? other, TypeComparison comparison)
    {
        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(this, other);
        }

        return other is DirectiveTypeDefinition otherDirective
            // TODO: Compare locations
            && otherDirective.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a new instance of <see cref="DirectiveTypeDefinition"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the directive type definition.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="DirectiveTypeDefinition"/>.
    /// </returns>
    public static DirectiveTypeDefinition Create(string name) => new(name);
}
