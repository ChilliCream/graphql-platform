using System.Text.Json;
using HotChocolate.Language;
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
        => valueLiteral.Kind is SyntaxKind.IntValue;

    /// <inheritdoc />
    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.Number;

    /// <inheritdoc />
    public override bool IsInstanceOfType(object runtimeValue)
    {
        if (runtimeValue is TRuntimeType value)
        {
            return value.CompareTo(MinValue) != -1
                && value.CompareTo(MaxValue) != 1;
        }

        return false;
    }

    /// <inheritdoc />
    public sealed override object CoerceInputLiteral(IValueNode valueLiteral)
    {
        if (valueLiteral is IntValueNode intLiteral && IsInstanceOfType(intLiteral))
        {
            return OnCoerceInputLiteral(intLiteral);
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
    public sealed override object CoerceInputValue(JsonElement inputValue)
    {
        if (inputValue.ValueKind is JsonValueKind.Number)
        {
            var value = OnCoerceInputValue(inputValue);
            if (value.CompareTo(MinValue) != -1
                && value.CompareTo(MaxValue) != 1)
            {
                return value;
            }
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

    /// <summary>
    /// Creates the exception to throw when <see cref="CoerceInputValue(JsonElement)"/>
    /// encounters an incompatible input value.
    /// </summary>
    /// <param name="inputValue">
    /// The input value that could not be coerced.
    /// </param>
    /// <returns>
    /// Returns the exception to throw.
    /// </returns>
    protected virtual LeafCoercionException CreateCoerceInputValueError(JsonElement inputValue)
        => Scalar_Cannot_CoerceInputValue(this, inputValue);

    /// <summary>
    /// Creates the exception to throw when <see cref="CoerceInputLiteral(IValueNode)"/>
    /// encounters an incompatible <see cref="IValueNode"/>.
    /// </summary>
    /// <param name="literal">
    /// The value syntax that could not be coerced.
    /// </param>
    /// <returns>
    /// Returns the exception to throw.
    /// </returns>
    protected virtual LeafCoercionException CreateCoerceInputLiteralError(IValueNode literal)
        => Scalar_Cannot_CoerceInputLiteral(this, literal);
}
