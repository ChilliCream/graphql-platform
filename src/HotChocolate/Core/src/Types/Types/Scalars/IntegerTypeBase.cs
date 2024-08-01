using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types;

public abstract class IntegerTypeBase<TRuntimeType>
    : ScalarType<TRuntimeType, IntValueNode>
    where TRuntimeType : IComparable
{
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

    public TRuntimeType MinValue { get; }

    public TRuntimeType MaxValue { get; }

    protected override bool IsInstanceOfType(IntValueNode valueSyntax)
    {
        try
        {
            return IsInstanceOfType(ParseLiteral(valueSyntax));
        }
        catch (InvalidFormatException)
        {
            return false;
        }
    }

    protected override bool IsInstanceOfType(TRuntimeType runtimeValue)
    {
        if (runtimeValue.CompareTo(MinValue) < 0 || runtimeValue.CompareTo(MaxValue) > 0)
        {
            return false;
        }

        return true;
    }

    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
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

        if (TryConvertSerialized(resultValue, ValueKind.Integer, out TRuntimeType c)
            && IsInstanceOfType(c))
        {
            runtimeValue = c;
            return true;
        }

        runtimeValue = null;
        return false;
    }

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
    protected virtual SerializationException CreateParseResultError(object runtimeValue)
    {
        return new(
            TypeResourceHelper.Scalar_Cannot_ParseResult(Name, runtimeValue.GetType()),
            this);
    }
}
