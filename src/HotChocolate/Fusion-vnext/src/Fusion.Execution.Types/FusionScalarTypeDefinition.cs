using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Represents a GraphQL scalar type definition in a fusion schema.
/// </summary>
public sealed class FusionScalarTypeDefinition : IScalarTypeDefinition, IFusionTypeDefinition
{
    private FusionDirectiveCollection _directives = null!;
    private bool _completed;

    /// <summary>
    /// Initializes a new instance of <see cref="FusionScalarTypeDefinition"/>.
    /// </summary>
    /// <param name="name">The name of the scalar type.</param>
    /// <param name="description">The description of the scalar type.</param>
    /// <param name="isInaccessible">A value indicating whether the scalar type is marked as inaccessible.</param>
    public FusionScalarTypeDefinition(
        string name,
        string? description,
        bool isInaccessible)
    {
        name.EnsureGraphQLName();

        Name = name;
        Description = description;
        IsInaccessible = isInaccessible;

        // these properties are initialized
        // in the type complete step.
        Features = null!;
    }

    /// <summary>
    /// Gets the kind of this type.
    /// </summary>
    public TypeKind Kind => TypeKind.Scalar;

    /// <summary>
    /// Gets the name of this scalar type.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of this scalar type.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the schema coordinate of this scalar type.
    /// </summary>
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    Type IRuntimeTypeProvider.RuntimeType => typeof(object);

    /// <summary>
    /// Gets a value indicating whether this scalar type is marked as inaccessible.
    /// </summary>
    public bool IsInaccessible { get; }

    /// <summary>
    /// Gets the directives applied to this scalar type.
    /// </summary>
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

    /// <summary>
    /// Gets the URL that specifies the behavior of this scalar type.
    /// </summary>
    public Uri? SpecifiedBy { get; private set; }

    /// <summary>
    /// Gets the serialization type for this scalar.
    /// </summary>
    public ScalarSerializationType SerializationType { get; private set; }

    /// <summary>
    /// Gets the pattern for this scalar type, if applicable.
    /// </summary>
    public string? Pattern { get; private set; }

    /// <summary>
    /// Gets the value kind that this scalar type can represent.
    /// </summary>
    public ScalarValueKind ValueKind { get; private set; }

    /// <summary>
    /// Gets the feature collection associated with this scalar type.
    /// </summary>
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

        if (context.Directives is null)
        {
            throw ThrowHelper.InvalidCompletionContext();
        }

        Directives = context.Directives;
        ValueKind = context.ValueKind;
        SpecifiedBy = context.SpecifiedBy;

        // if the value kind is any, we need to determine the value kind based on the name
        // for the spec scalars.
        if (ValueKind is ScalarValueKind.Any)
        {
            ValueKind = Name switch
            {
                "ID" => ScalarValueKind.String | ScalarValueKind.Integer,
                "String" => ScalarValueKind.String,
                "Int" => ScalarValueKind.Integer,
                "Float" => ScalarValueKind.Float,
                "Boolean" => ScalarValueKind.Boolean,
                _ => ScalarValueKind.Any
            };
        }

        SerializationType = context.SerializationType;
        Pattern = context.Pattern;

        _completed = true;
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

    /// <inheritdoc />
    public bool IsValueCompatible(IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(valueLiteral);

        if (ValueKind == ScalarValueKind.Any)
        {
            return true;
        }

        return valueLiteral.Kind switch
        {
            SyntaxKind.NullValue => true,
            SyntaxKind.EnumValue => false,
            SyntaxKind.StringValue => ValueKind.HasFlag(ScalarValueKind.String),
            SyntaxKind.IntValue => ValueKind.HasFlag(ScalarValueKind.Integer),
            SyntaxKind.FloatValue => ValueKind.HasFlag(ScalarValueKind.Float),
            SyntaxKind.BooleanValue => ValueKind.HasFlag(ScalarValueKind.Boolean),
            SyntaxKind.ListValue => ValueKind.HasFlag(ScalarValueKind.List),
            SyntaxKind.ObjectValue => ValueKind.HasFlag(ScalarValueKind.Object),
            _ => false
        };
    }

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
}
