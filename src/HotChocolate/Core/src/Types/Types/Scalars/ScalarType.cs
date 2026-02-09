using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Text.Json;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Utilities;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types;

/// <summary>
/// Scalar types represent primitive leaf values in a GraphQL type system.
/// GraphQL responses take the form of a hierarchical tree;
/// the leaves on these trees are GraphQL scalars.
/// </summary>
public abstract partial class ScalarType
    : TypeSystemObject<ScalarTypeConfiguration>
    , IScalarTypeDefinition
    , ILeafType
{
    /// <summary>
    /// Gets the type kind.
    /// </summary>
    public TypeKind Kind => TypeKind.Scalar;

    /// <summary>
    /// Defines if this scalar binds implicitly to its runtime type or
    /// if it has to be explicitly assigned to it.
    /// </summary>
    public BindingBehavior Bind { get; }

    /// <summary>
    /// The .NET type representation of this scalar.
    /// </summary>
    public abstract Type RuntimeType { get; }

    /// <summary>
    /// Gets the schema coordinate of this scalar type.
    /// </summary>
    public SchemaCoordinate Coordinate => new(Name, ofDirective: false);

    /// <summary>
    /// Gets the optional description of this scalar type.
    /// </summary>
    public Uri? SpecifiedBy
    {
        get;
        protected set
        {
            if (IsExecutable)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeSystem_Immutable);
            }
            field = value;
        }
    }

    /// <inheritdoc />
    public abstract ScalarSerializationType SerializationType { get; }

    /// <inheritdoc />
    public string? Pattern
    {
        get;
        protected set
        {
            if (IsExecutable)
            {
                throw new InvalidOperationException(
                    TypeResources.TypeSystem_Immutable);
            }
            field = value;
        }
    }

    /// <summary>
    /// Gets the directives of this scalar type.
    /// </summary>
    public DirectiveCollection Directives { get; private set; }

    IReadOnlyDirectiveCollection IDirectivesProvider.Directives
        => Directives.AsReadOnlyDirectiveCollection();

    /// <summary>
    /// Provides access to the schema type converter.
    /// </summary>
    protected ITypeConverter Converter => _converter;

    /// <summary>
    /// Gets a value indicating whether the <c>@serializeAs</c> directive should be applied to this scalar type.
    /// </summary>
    protected virtual bool ApplySerializeAsToScalars => true;

    /// <summary>
    /// Defines if the specified <paramref name="type"/> is assignable from the current <see cref="ScalarType"/>.
    /// </summary>
    /// <param name="type">
    /// The type that shall be checked.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="type"/> is assignable from the current <see cref="ScalarType"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool IsAssignableFrom(ITypeDefinition type)
        => ReferenceEquals(type, this);

    /// <summary>
    /// Defines if the specified <paramref name="other"/> is equal to the current <see cref="ScalarType"/>.
    /// </summary>
    /// <param name="other">
    /// The other scalar type.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified <paramref name="other"/> is equal to the current <see cref="ScalarType"/>;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(IType? other) => ReferenceEquals(other, this);

    /// <inheritdoc cref="IScalarTypeDefinition.IsValueCompatible" />
    public virtual bool IsValueCompatible(IValueNode valueLiteral)
    {
        if ((SerializationType & ScalarSerializationType.String) == ScalarSerializationType.String
            && valueLiteral is { Kind: SyntaxKind.StringValue })
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Int) == ScalarSerializationType.Int
            && valueLiteral is { Kind: SyntaxKind.IntValue })
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Float) == ScalarSerializationType.Float
            && valueLiteral is { Kind: SyntaxKind.FloatValue })
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Boolean) == ScalarSerializationType.Boolean
            && valueLiteral is { Kind: SyntaxKind.BooleanValue })
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.List) == ScalarSerializationType.List
            && valueLiteral is { Kind: SyntaxKind.ListValue })
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Object) == ScalarSerializationType.Object
            && valueLiteral is { Kind: SyntaxKind.ObjectValue })
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public virtual bool IsValueCompatible(JsonElement inputValue)
    {
        if ((SerializationType & ScalarSerializationType.String) == ScalarSerializationType.String
            && inputValue.ValueKind == JsonValueKind.String)
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Int) == ScalarSerializationType.Int
            && inputValue.ValueKind == JsonValueKind.Number)
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Float) == ScalarSerializationType.Float
            && inputValue.ValueKind == JsonValueKind.Number)
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Boolean) == ScalarSerializationType.Boolean
            && inputValue.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.List) == ScalarSerializationType.List
            && inputValue.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        if ((SerializationType & ScalarSerializationType.Object) == ScalarSerializationType.Object
            && inputValue.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public abstract object CoerceInputLiteral(IValueNode valueLiteral);

    /// <inheritdoc />
    public abstract object CoerceInputValue(JsonElement inputValue, IFeatureProvider context);

    /// <inheritdoc />
    public abstract void CoerceOutputValue(object runtimeValue, ResultElement resultValue);

    /// <inheritdoc />
    public abstract IValueNode ValueToLiteral(object runtimeValue);

    /// <summary>
    /// Returns a string that represents the current <see cref="ScalarType"/>.
    /// </summary>
    /// <returns>
    /// A string that represents the current <see cref="ScalarType"/>.
    /// </returns>
    public override string ToString() => Format(this).ToString();

    /// <summary>
    /// Creates a <see cref="ScalarTypeDefinitionNode"/> from the current <see cref="ScalarType"/>.
    /// </summary>
    /// <returns>
    /// Returns a <see cref="ScalarTypeDefinitionNode"/>.
    /// </returns>
    public ScalarTypeDefinitionNode ToSyntaxNode() => Format(this);

    ISyntaxNode ISyntaxNodeProvider.ToSyntaxNode() => ToSyntaxNode();
}
