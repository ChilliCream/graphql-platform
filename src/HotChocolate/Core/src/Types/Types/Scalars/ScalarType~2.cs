using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// Scalar types represent primitive leaf values in a GraphQL type system.
/// GraphQL responses take the form of a hierarchical tree;
/// the leaves on these trees are GraphQL scalars.
/// </summary>
public abstract class ScalarType<TRuntimeType, TLiteral> : ScalarType<TRuntimeType> where TLiteral : IValueNode
{
    /// <inheritdoc />
    protected ScalarType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    /// <inheritdoc />
    public override ScalarSerializationType SerializationType
    {
        get
        {
            if (typeof(TLiteral) == typeof(StringValueNode))
            {
                return ScalarSerializationType.String;
            }

            if (typeof(TLiteral) == typeof(IntValueNode))
            {
                return ScalarSerializationType.Int;
            }

            if (typeof(TLiteral) == typeof(FloatValueNode))
            {
                return ScalarSerializationType.Float;
            }

            if (typeof(TLiteral) == typeof(BooleanValueNode))
            {
                return ScalarSerializationType.Boolean;
            }

            if (typeof(TLiteral) == typeof(ObjectValueNode))
            {
                return ScalarSerializationType.Object;
            }

            if (typeof(TLiteral) == typeof(ListValueNode))
            {
                return ScalarSerializationType.List;
            }

            throw new NotSupportedException();
        }
    }

    /// <inheritdoc />
    public sealed override bool IsValueCompatible(IValueNode valueSyntax)
    {
        ArgumentNullException.ThrowIfNull(valueSyntax);

        if (valueSyntax.Kind == SyntaxKind.NullValue)
        {
            return true;
        }

        return valueSyntax is TLiteral;
    }

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
    {
        if (inputValue.ValueKind == JsonValueKind.Undefined)
        {
            return false;
        }

        if (inputValue.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (SerializationType == ScalarSerializationType.String
            && inputValue.ValueKind == JsonValueKind.String)
        {
            return true;
        }

        if (SerializationType == ScalarSerializationType.Int
            && inputValue.ValueKind == JsonValueKind.Number)
        {
            return true;
        }

        if (SerializationType == ScalarSerializationType.Float
            && inputValue.ValueKind == JsonValueKind.Number)
        {
            return true;
        }

        if (SerializationType == ScalarSerializationType.Boolean
            && (inputValue.ValueKind == JsonValueKind.True
                || inputValue.ValueKind == JsonValueKind.False))
        {
            return true;
        }

        if (SerializationType == ScalarSerializationType.Object
            && inputValue.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        if (SerializationType == ScalarSerializationType.List
            && inputValue.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public sealed override object? CoerceInputLiteral(IValueNode valueLiteral)
    {
        ArgumentNullException.ThrowIfNull(valueLiteral);

        if (valueLiteral.Kind is SyntaxKind.NullValue)
        {
            return null;
        }

        if (valueLiteral is TLiteral literal)
        {
            return CoerceInputLiteral(literal);
        }

        throw CreateCoerceInputLiteralError(valueLiteral);
    }

    public abstract object? CoerceInputLiteral(TLiteral valueLiteral);

    /// <summary>
    /// Creates the exception that will be thrown when <see cref="CoerceInputLiteral(IValueNode)"/> encountered an
    /// invalid <see cref="IValueNode "/>
    /// </summary>
    /// <param name="valueSyntax">
    /// The value syntax that should be parsed
    /// </param>
    /// <returns>
    /// The created exception that should be thrown
    /// </returns>
    protected virtual LeafCoercionException CreateCoerceInputLiteralError(IValueNode valueSyntax)
        => new LeafCoercionException(
            TypeResourceHelper.Scalar_Cannot_CoerceInputLiteral(Name, valueSyntax.GetType()),
            this);
}
