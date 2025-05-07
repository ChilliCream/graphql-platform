using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public sealed class FusionScalarTypeDefinition : IScalarTypeDefinition
{
    private FusionDirectiveCollection _directives = default!;
    private bool _completed;

    public FusionScalarTypeDefinition(string name,
        string? description,
        ScalarResultType scalarResultType = ScalarResultType.Unknown)
    {
        Name = name;
        Description = description;
        ScalarResultType = scalarResultType;

        if (scalarResultType is ScalarResultType.Unknown)
        {
            ScalarResultType = name switch
            {
                "ID" => ScalarResultType.String | ScalarResultType.Int,
                "String" => ScalarResultType.String,
                "Int" => ScalarResultType.Int,
                "Float" => ScalarResultType.Float,
                "Boolean" => ScalarResultType.Boolean,
                _ => ScalarResultType.Unknown
            };
        }

        // these properties are initialized
        // in the type complete step.
        Features = null!;
    }

    public TypeKind Kind => TypeKind.Scalar;

    public string Name { get; }

    public string? Description { get; }

    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    public FusionDirectiveCollection Directives
    {
        get => _directives;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            _directives = value;
        }
    }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => _directives;

    public ScalarResultType ScalarResultType { get; }

    public IFeatureCollection Features
    {
        get;
        private set
        {
            ThrowHelper.EnsureNotSealed(_completed);
            field = value;
        }
    }

    internal void Complete(CompositeScalarTypeCompletionContext context)
    {
        ThrowHelper.EnsureNotSealed(_completed);
        Directives = context.Directives;
        _completed = true;
    }

    /// <summary>
    /// Gets the string representation of this instance.
    /// </summary>
    /// <returns>
    /// The string representation of this instance.
    /// </returns>
    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(true);

    /// <summary>
    /// Creates a <see cref="ScalarTypeDefinitionNode"/>
    /// from a <see cref="FusionScalarTypeDefinition"/>.
    /// </summary>
    public ScalarTypeDefinitionNode ToSyntaxNode()
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

        return other is FusionScalarTypeDefinition otherScalar
            && otherScalar.Name.Equals(Name, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool IsAssignableFrom(ITypeDefinition type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (type.Kind == TypeKind.Scalar)
        {
            return Equals(type, TypeComparison.Reference);
        }

        return false;
    }
}
