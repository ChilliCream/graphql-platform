using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types;

public abstract class FloatTypeBase<TRuntimeType>
    : ScalarType<TRuntimeType>
    where TRuntimeType : IComparable
{
    protected FloatTypeBase(
        string name,
        TRuntimeType min,
        TRuntimeType max,
        BindingBehavior bind = BindingBehavior.Explicit)
       : base(name, bind)
    {
        MinValue = min;
        MaxValue = max;
    }

    public TRuntimeType MinValue { get; }

    public TRuntimeType MaxValue { get; }

    public override bool IsValueCompatible(IValueNode valueSyntax)
    {
        ArgumentNullException.ThrowIfNull(valueSyntax);

        if (valueSyntax is NullValueNode)
        {
            return true;
        }

        if (valueSyntax is FloatValueNode floatLiteral && IsInstanceOfType(floatLiteral))
        {
            return true;
        }

        // Input coercion rules specify that float values can be coerced
        // from IntValueNode and FloatValueNode:
        // http://facebook.github.io/graphql/June2018/#sec-Float
        if (valueSyntax is IntValueNode intLiteral && IsInstanceOfType(intLiteral))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public sealed override bool IsInstanceOfType(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return true;
        }

        if (runtimeValue is TRuntimeType t)
        {
            return IsInstanceOfType(t);
        }

        return false;
    }

    protected virtual bool IsInstanceOfType(IFloatValueLiteral valueSyntax)
    {
        return IsInstanceOfType(ParseLiteral(valueSyntax));
    }

    protected virtual bool IsInstanceOfType(TRuntimeType value)
    {
        if (value.CompareTo(MinValue) == -1 || value.CompareTo(MaxValue) == 1)
        {
            return false;
        }

        return true;
    }

    public override object? CoerceInputLiteral(IValueNode valueSyntax)
    {
        ArgumentNullException.ThrowIfNull(valueSyntax);

        if (valueSyntax is NullValueNode)
        {
            return null;
        }

        if (valueSyntax is FloatValueNode floatLiteral && IsInstanceOfType(floatLiteral))
        {
            return ParseLiteral(floatLiteral);
        }

        // Input coercion rules specify that float values can be coerced
        // from IntValueNode and FloatValueNode:
        // http://facebook.github.io/graphql/June2018/#sec-Float

        if (valueSyntax is IntValueNode intLiteral && IsInstanceOfType(intLiteral))
        {
            return ParseLiteral(intLiteral);
        }

        throw CreateParseLiteralError(valueSyntax);
    }

    protected abstract TRuntimeType ParseLiteral(IFloatValueLiteral valueSyntax);

    public override IValueNode CoerceInputValue(object? runtimeValue)
    {
        if (runtimeValue is null)
        {
            return NullValueNode.Default;
        }

        if (runtimeValue is TRuntimeType casted && IsInstanceOfType(casted))
        {
            return ParseValue(casted);
        }

        throw CreateParseValueError(runtimeValue);
    }

    protected abstract FloatValueNode ParseValue(TRuntimeType runtimeValue);

    public sealed override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is TRuntimeType casted && IsInstanceOfType(casted))
        {
            return ParseValue(casted);
        }

        if (TryConvertSerialized(resultValue, ValueKind.Integer, out TRuntimeType c)
            && IsInstanceOfType(c))
        {
            return ParseValue(c);
        }

        throw CreateParseResultError(resultValue);
    }

    public override bool TryCoerceOutputValue(object? runtimeValue, out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is TRuntimeType casted && IsInstanceOfType(casted))
        {
            resultValue = runtimeValue;
            return true;
        }

        resultValue = null;
        return false;
    }

    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is TRuntimeType casted && IsInstanceOfType(casted))
        {
            runtimeValue = resultValue;
            return true;
        }

        if ((TryConvertSerialized(resultValue, ValueKind.Float, out TRuntimeType c)
            || TryConvertSerialized(resultValue, ValueKind.Integer, out c))
            && IsInstanceOfType(c))
        {
            runtimeValue = c;
            return true;
        }

        runtimeValue = null;
        return false;
    }

    /// <summary>
    /// Creates the exception that will be thrown when <see cref="CoerceInputValue(object?)"/>
    /// encountered an invalid runtime value.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value.
    /// </param>
    /// <returns>
    /// The created exception that should be thrown
    /// </returns>
    protected virtual LeafCoercionException CreateParseValueError(object runtimeValue)
        => new(TypeResourceHelper.Scalar_Cannot_ParseResult(Name, runtimeValue.GetType()), this);

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
    protected virtual LeafCoercionException CreateParseLiteralError(IValueNode valueSyntax)
        => new(TypeResourceHelper.Scalar_Cannot_CoerceInputLiteral(Name, valueSyntax.GetType()), this);

    /// <summary>
    /// Creates the exception that will be thrown when <see cref="ParseResult"/> encountered an
    /// invalid value
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtimeValue that should be parsed
    /// </param>
    /// <returns>
    /// The created exception that should be thrown
    /// </returns>
    protected virtual LeafCoercionException CreateParseResultError(object runtimeValue)
        => new(TypeResourceHelper.Scalar_Cannot_ParseResult(Name, runtimeValue.GetType()), this);
}
