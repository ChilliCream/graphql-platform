using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types;

/// <summary>
/// Base class for integer scalar types with min/max value constraints.
/// </summary>
/// <typeparam name="TRuntimeType">
/// The .NET runtime type that this scalar represents.
/// </typeparam>
public abstract class IntegerTypeBase<TRuntimeType>
    : ScalarType<TRuntimeType>
    where TRuntimeType : IComparable
{
    /// <summary>
    /// Initializes a new instance of <see cref="IntegerTypeBase{TRuntimeType}"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the scalar type.
    /// </param>
    /// <param name="min">
    /// The minimum allowed value.
    /// </param>
    /// <param name="max">
    /// The maximum allowed value.
    /// </param>
    /// <param name="bind">
    /// The binding behavior of this scalar.
    /// </param>
    protected IntegerTypeBase(
        string name,
        TRuntimeType min,
        TRuntimeType max,
        BindingBehavior bind = BindingBehavior.Explicit)
       : base(name, bind)
    {
        MinValue = min;
        MaxValue = max;
    }

    /// <inheritdoc />
    public override ScalarSerializationType SerializationType => ScalarSerializationType.Int;

    /// <summary>
    /// Gets the minimum allowed value for this scalar.
    /// </summary>
    public TRuntimeType MinValue { get; }

    /// <summary>
    /// Gets the maximum allowed value for this scalar.
    /// </summary>
    public TRuntimeType MaxValue { get; }

    /// <inheritdoc />
    public override bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is { Kind: SyntaxKind.IntValue };

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.Number;

    /// <inheritdoc />
    public sealed override object CoerceInputLiteral(IValueNode valueLiteral)
    {
        if (valueLiteral is IntValueNode intLiteral)
        {
            TRuntimeType runtimeValue;

            try
            {
                runtimeValue = OnCoerceInputLiteral(intLiteral);
            }
            catch (Exception ex)
            {
                throw CreateCoerceInputLiteralError(valueLiteral, ex);
            }

            AssertFormat(runtimeValue);
            return runtimeValue;
        }

        throw CreateCoerceInputLiteralError(valueLiteral);
    }

    /// <summary>
    /// Coerces an int literal into the runtime value.
    /// </summary>
    /// <param name="valueLiteral">
    /// The int literal to coerce.
    /// </param>
    /// <returns>
    /// Returns the runtime value representation.
    /// </returns>
    protected abstract TRuntimeType OnCoerceInputLiteral(IntValueNode valueLiteral);

    /// <inheritdoc />
    public sealed override object CoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (inputValue.ValueKind is JsonValueKind.Number)
        {
            TRuntimeType runtimeValue;

            try
            {
                runtimeValue = OnCoerceInputValue(inputValue);
            }
            catch (Exception ex)
            {
                throw CreateCoerceInputValueError(inputValue, ex);
            }

            AssertFormat(runtimeValue);
            return runtimeValue;
        }

        throw CreateCoerceInputValueError(inputValue);
    }

    /// <summary>
    /// Coerces a JSON number into the runtime value.
    /// </summary>
    /// <param name="inputValue">
    /// The JSON input value to coerce.
    /// </param>
    /// <returns>
    /// Returns the runtime value representation.
    /// </returns>
    protected abstract TRuntimeType OnCoerceInputValue(JsonElement inputValue);

    /// <inheritdoc />
    public override void CoerceOutputValue(object runtimeValue, ResultElement resultValue)
    {
        if (runtimeValue is TRuntimeType castedRuntimeValue)
        {
            AssertFormat(castedRuntimeValue);
            OnCoerceOutputValue(castedRuntimeValue, resultValue);
            return;
        }

        throw Scalar_Cannot_CoerceOutputValue(this, runtimeValue);
    }

    /// <inheritdoc />
    public override IValueNode ValueToLiteral(object runtimeValue)
    {
        if (runtimeValue is TRuntimeType castedRuntimeValue)
        {
            AssertFormat(castedRuntimeValue);
            return OnValueToLiteral(castedRuntimeValue);
        }

        throw CreateValueToLiteralError(runtimeValue);
    }

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
        IValueNode valueLiteral,
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
    /// Creates the exception to throw when a runtime value is outside
    /// the allowed min/max range.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value that is out of range.
    /// </param>
    /// <returns>
    /// Returns the exception to throw.
    /// </returns>
    protected virtual LeafCoercionException FormatError(TRuntimeType runtimeValue)
        => Scalar_FormatIsInvalid(this, runtimeValue);

    /// <summary>
    /// Validates that the runtime value is within the allowed min/max range.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value to validate.
    /// </param>
    /// <exception cref="LeafCoercionException">
    /// Thrown when the value is less than <see cref="MinValue"/> or greater than <see cref="MaxValue"/>.
    /// </exception>
    private void AssertFormat(TRuntimeType runtimeValue)
    {
        if (runtimeValue.CompareTo(MinValue) < 0 || runtimeValue.CompareTo(MaxValue) > 0)
        {
            throw FormatError(runtimeValue);
        }
    }
}
