using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

/// <summary>
/// Represents a GraphQL scalar type definition.
/// </summary>
public class MutableScalarTypeDefinition : INamedTypeSystemMemberDefinition<MutableScalarTypeDefinition>
    , IScalarTypeDefinition
    , IMutableTypeDefinition
{
    private DirectiveCollection? _directives;

    /// <summary>
    /// Represents a GraphQL scalar type definition.
    /// </summary>
    public MutableScalarTypeDefinition(string name)
    {
        Name = name.EnsureGraphQLName();
    }

    /// <inheritdoc />
    public TypeKind Kind => TypeKind.Scalar;

    /// <inheritdoc cref="IMutableTypeDefinition.Name" />
    public string Name
    {
        get;
        set => field = value.EnsureGraphQLName();
    }

    /// <inheritdoc cref="IMutableTypeDefinition.Description" />
    public string? Description { get; set; }

    /// <inheritdoc />
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    Type IRuntimeTypeProvider.RuntimeType => typeof(object);

    /// <inheritdoc cref="IMutableTypeDefinition.IsIntrospectionType" />
    public bool IsIntrospectionType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this scalar type is a spec scalar.
    /// </summary>
    public bool IsSpecScalar { get; set; }

    public DirectiveCollection Directives
        => _directives ??= [];

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives ?? EmptyCollections.Directives;

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IFeatureCollection Features
        => field ??= new FeatureCollection();

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

        return other is MutableScalarTypeDefinition otherScalar
            && otherScalar.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool IsAssignableFrom(ITypeDefinition type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.Kind == TypeKind.Scalar)
        {
            return Equals(type, TypeComparison.Reference);
        }

        return false;
    }

    public Uri? SpecifiedBy
    {
        get
        {
            var specifiedBy = Directives.FirstOrDefault("specifiedBy");

            if (specifiedBy is null)
            {
                return null;
            }

            var url = specifiedBy.Arguments.First(t => t.Name.Equals("url", StringComparison.Ordinal));

            if (url.Value is not StringValueNode urlValue)
            {
                throw new InvalidOperationException("The specified URL is not a valid URI.");
            }

            return new Uri(urlValue.Value);
        }
    }

    /// <inheritdoc />
    public ScalarSerializationType SerializationType { get; set; }

    /// <inheritdoc />
    public string? Pattern { get; set; }

    /// <inheritdoc />
    public bool IsValueCompatible(IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(valueLiteral);
        return true;
    }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => Format(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="ScalarTypeDefinitionNode"/> from a <see cref="MutableScalarTypeDefinition"/>.
    /// </summary>
    public ScalarTypeDefinitionNode ToSyntaxNode() => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => Format(this);

    /// <summary>
    /// Creates a new instance of <see cref="MutableScalarTypeDefinition"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the scalar type definition.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="MutableScalarTypeDefinition"/>.
    /// </returns>
    public static MutableScalarTypeDefinition Create(string name) => new(name);
}
