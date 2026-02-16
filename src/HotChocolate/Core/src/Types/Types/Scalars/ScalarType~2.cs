using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
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
    : ScalarType
    where TRuntimeType : notnull
    where TLiteral : IValueNode
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly ScalarSerializationType s_serializationType = DetermineSerializationType();

    /// <inheritdoc />
    protected ScalarType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    /// <inheritdoc />
    public sealed override Type RuntimeType => typeof(TRuntimeType);

    /// <inheritdoc />
    public sealed override ScalarSerializationType SerializationType => s_serializationType;

    /// <inheritdoc />
    public sealed override object CoerceInputLiteral(IValueNode valueLiteral)
    {
        if (valueLiteral is TLiteral literal)
        {
            TRuntimeType runtimeValue;

            try
            {
                runtimeValue = OnCoerceInputLiteral(literal);
            }
            catch (LeafCoercionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw CreateCoerceInputLiteralError(valueLiteral, ex);
            }

            return runtimeValue;
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
    /// Returns the runtime value representation of type <typeparamref name="TRuntimeType"/>.
    /// </returns>
    /// <exception cref="LeafCoercionException">
    /// Unable to coerce the given <paramref name="valueLiteral"/> into a runtime value.
    /// </exception>
    protected abstract TRuntimeType OnCoerceInputLiteral(TLiteral valueLiteral);

    /// <inheritdoc />
    public sealed override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        switch (s_serializationType)
        {
            case ScalarSerializationType.String
                when inputValue.ValueKind is not JsonValueKind.String:
            case ScalarSerializationType.Int
                when inputValue.ValueKind is not JsonValueKind.Number:
            case ScalarSerializationType.Float
                when inputValue.ValueKind is not JsonValueKind.Number:
            case ScalarSerializationType.Boolean
                when inputValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False):
            case ScalarSerializationType.Object
                when inputValue.ValueKind is not JsonValueKind.Object:
            case ScalarSerializationType.List
                when inputValue.ValueKind is not JsonValueKind.Array:
                throw CreateCoerceInputValueError(inputValue);

            default:
                TRuntimeType runtimeValue;

                try
                {
                    runtimeValue = OnCoerceInputValue(inputValue, context);
                }
                catch (LeafCoercionException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw CreateCoerceInputValueError(inputValue, ex);
                }

                return runtimeValue;
        }
    }

    /// <summary>
    /// Coerces a JSON input value into a runtime value.
    /// </summary>
    /// <param name="inputValue">
    /// The JSON element to coerce.
    /// </param>
    /// <param name="context">
    /// The feature provider context for accessing additional services.
    /// </param>
    /// <returns>
    /// Returns the runtime value representation of type <typeparamref name="TRuntimeType"/>.
    /// </returns>
    /// <exception cref="LeafCoercionException">
    /// Unable to coerce the given <paramref name="inputValue"/> into a runtime value.
    /// </exception>
    /// <remarks>
    /// This method is called after the JSON value kind has been validated to match the expected
    /// serialization type. The implementation should parse the JSON value and convert it to the
    /// runtime type.
    /// </remarks>
    protected abstract TRuntimeType OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context);

    /// <inheritdoc />
    public sealed override void CoerceOutputValue(object runtimeValue, ResultElement resultValue)
    {
        if (runtimeValue is TRuntimeType castedRuntimeValue)
        {
            OnCoerceOutputValue(castedRuntimeValue, resultValue);
            return;
        }

        throw CreateCoerceOutputValueError(runtimeValue);
    }

    /// <summary>
    /// Coerces a runtime value into a result value for serialization.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value to serialize.
    /// </param>
    /// <param name="resultValue">
    /// The result element to write the serialized value to.
    /// </param>
    /// <exception cref="LeafCoercionException">
    /// Unable to coerce the given <paramref name="runtimeValue"/> into a result value.
    /// </exception>
    /// <remarks>
    /// This method is responsible for writing the runtime value to the result element using
    /// appropriate methods like <c>SetStringValue</c>, <c>SetNumberValue</c>, etc.
    /// </remarks>
    protected abstract void OnCoerceOutputValue(TRuntimeType runtimeValue, ResultElement resultValue);

    /// <inheritdoc />
    public sealed override IValueNode ValueToLiteral(object runtimeValue)
    {
        if (runtimeValue is TRuntimeType literal)
        {
            return OnValueToLiteral(literal);
        }

        throw CreateValueToLiteralError(runtimeValue);
    }

    /// <summary>
    /// Converts a runtime value into a GraphQL literal (AST value node).
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value to convert.
    /// </param>
    /// <returns>
    /// Returns the GraphQL literal representation of type <typeparamref name="TLiteral"/>.
    /// </returns>
    /// <exception cref="LeafCoercionException">
    /// Unable to convert the given <paramref name="runtimeValue"/> into a literal.
    /// </exception>
    /// <remarks>
    /// This method is typically used for query rewriting and value serialization scenarios
    /// where a runtime value needs to be represented as a GraphQL AST node.
    /// </remarks>
    protected abstract TLiteral OnValueToLiteral(TRuntimeType runtimeValue);

    /// <summary>
    /// Creates the exception to throw when <see cref="CoerceInputLiteral(IValueNode)"/>
    /// encounters an incompatible <see cref="IValueNode"/>.
    /// </summary>
    /// <param name="valueLiteral">
    /// The value syntax that could not be coerced.
    /// </param>
    /// <param name="error">
    /// An optional exception that was thrown during coercion.
    /// </param>
    /// <returns>
    /// Returns the exception to throw.
    /// </returns>
    protected virtual LeafCoercionException CreateCoerceInputLiteralError(
        IValueNode? valueLiteral,
        Exception? error = null)
        => Scalar_Cannot_CoerceInputLiteral(this, valueLiteral, error);

    /// <summary>
    /// Creates the exception to throw when <see cref="CoerceInputValue(JsonElement, IFeatureProvider)"/>
    /// encounters an incompatible <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="inputValue">
    /// The JSON value that could not be coerced.
    /// </param>
    /// <param name="error">
    /// An optional exception that was thrown during coercion.
    /// </param>
    /// <returns>
    /// Returns the exception to throw.
    /// </returns>
    protected virtual LeafCoercionException CreateCoerceInputValueError(
        JsonElement inputValue,
        Exception? error = null)
        => Scalar_Cannot_CoerceInputValue(this, inputValue, error);

    /// <summary>
    /// Creates the exception to throw when <see cref="CoerceOutputValue(object, ResultElement)"/>
    /// encounters an incompatible runtime value.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value that could not be coerced.
    /// </param>
    /// <returns>
    /// Returns the exception to throw.
    /// </returns>
    protected virtual LeafCoercionException CreateCoerceOutputValueError(object runtimeValue)
        => Scalar_Cannot_CoerceOutputValue(this, runtimeValue);

    /// <summary>
    /// Creates the exception to throw when <see cref="ValueToLiteral(object)"/>
    /// encounters an incompatible runtime value.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value that could not be converted to a literal.
    /// </param>
    /// <returns>
    /// Returns the exception to throw.
    /// </returns>
    protected virtual LeafCoercionException CreateValueToLiteralError(object runtimeValue)
        => Scalar_Cannot_ConvertValueToLiteral(this, runtimeValue);

    private static ScalarSerializationType DetermineSerializationType()
    {
        if (typeof(TLiteral) == typeof(StringValueNode))
        {
            return ScalarSerializationType.String;
        }
        else if (typeof(TLiteral) == typeof(IntValueNode))
        {
            return ScalarSerializationType.Int;
        }
        else if (typeof(TLiteral) == typeof(FloatValueNode))
        {
            return ScalarSerializationType.Float;
        }
        else if (typeof(TLiteral) == typeof(BooleanValueNode))
        {
            return ScalarSerializationType.Boolean;
        }
        else if (typeof(TLiteral) == typeof(ObjectValueNode))
        {
            return ScalarSerializationType.Object;
        }
        else if (typeof(TLiteral) == typeof(ListValueNode))
        {
            return ScalarSerializationType.List;
        }
        else
        {
            throw new InvalidOperationException("Invalid literal type.");
        }
    }
}
