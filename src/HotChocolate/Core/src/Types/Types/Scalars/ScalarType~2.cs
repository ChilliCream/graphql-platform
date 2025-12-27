using System.Text.Json;
using HotChocolate.Language;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// Scalar types represent primitive leaf values in a GraphQL type system.
/// GraphQL responses take the form of a hierarchical tree;
/// the leaves on these trees are GraphQL scalars.
/// </summary>
/// <typeparam name="TRuntimeType">
/// The .NET runtime type that this scalar represents.
/// </typeparam>
/// <typeparam name="TLiteral">
/// The GraphQL literal (AST value node) type that this scalar accepts.
/// </typeparam>
public abstract class ScalarType<TRuntimeType, TLiteral>
    : ScalarType<TRuntimeType>
    where TRuntimeType : notnull
    where TLiteral : IValueNode
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
        => valueSyntax is TLiteral;

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
    {
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
    public sealed override object CoerceInputLiteral(IValueNode valueLiteral)
    {
        if (valueLiteral is TLiteral literal)
        {
            return CoerceInputLiteral(literal);
        }

        throw CreateCoerceInputLiteralError(valueLiteral);
    }

    /// <summary>
    /// Coerces a GraphQL literal into a runtime value.
    /// </summary>
    /// <param name="valueLiteral">
    /// The GraphQL literal to coerce.
    /// </param>
    /// <returns>
    /// Returns the runtime value representation.
    /// </returns>
    /// <exception cref="LeafCoercionException">
    /// Unable to coerce the given <paramref name="valueLiteral"/> into a runtime value.
    /// </exception>
    public abstract object CoerceInputLiteral(TLiteral valueLiteral);

    /// <summary>
    /// Creates the exception to throw when <see cref="CoerceInputLiteral(IValueNode)"/>
    /// encounters an incompatible <see cref="IValueNode"/>.
    /// </summary>
    /// <param name="valueSyntax">
    /// The value syntax that could not be coerced.
    /// </param>
    /// <returns>
    /// Returns the exception to throw.
    /// </returns>
    protected virtual LeafCoercionException CreateCoerceInputLiteralError(IValueNode valueSyntax)
        => Scalar_Cannot_CoerceInputLiteral(this, valueSyntax);
}
