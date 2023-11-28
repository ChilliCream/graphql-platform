using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using static HotChocolate.Types.NodaTime.Properties.NodaTimeResources;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// This base class provides serialization functionality for noda time scalars
/// that have a <see cref="int"/> result value and a struct runtime value.
/// </summary>
/// <typeparam name="TRuntimeType">
/// The runtime type.
/// </typeparam>
public abstract class IntToStructBaseType<TRuntimeType>
    : ScalarType<TRuntimeType, IntValueNode>
    where TRuntimeType : struct
{
    /// <summary>
    /// Initializes a new instance of <see cref="IntToStructBaseType{TRuntimeType}"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the scalar.
    /// </param>
    protected IntToStructBaseType(string name)
        : base(name, BindingBehavior.Implicit)
    {
    }

    /// <inheritdoc />
    protected override TRuntimeType ParseLiteral(IntValueNode literal)
    {
        if (TryDeserialize(literal.ToInt32(), out TRuntimeType? value))
        {
            return value.Value;
        }

        throw new SerializationException(
            string.Format(IntToStructBaseType_ParseLiteral_UnableToDeserializeInt, Name),
            this);
    }

    /// <inheritdoc />
    protected override IntValueNode ParseValue(TRuntimeType value)
    {
        if (TrySerialize(value, out var val))
        {
            return new IntValueNode(val.Value);
        }

        throw new SerializationException(
            string.Format(IntToStructBaseType_ParseLiteral_UnableToDeserializeInt, Name),
            this);
    }

    /// <inheritdoc />
    public override IValueNode ParseResult(object? resultValue)
    {
        if (resultValue is null)
        {
            return NullValueNode.Default;
        }

        if (resultValue is int s)
        {
            return new IntValueNode(s);
        }

        if (resultValue is TRuntimeType v)
        {
            return ParseValue(v);
        }

        throw new SerializationException(
            string.Format(IntToStructBaseType_ParseLiteral_UnableToDeserializeInt, Name),
            this);
    }

    /// <inheritdoc />
    public override bool TrySerialize(
        object? runtimeValue,
        out object? resultValue)
    {
        if (runtimeValue is null)
        {
            resultValue = null;
            return true;
        }

        if (runtimeValue is TRuntimeType dt && TrySerialize(dt, out var val))
        {
            resultValue = val.Value;
            return true;
        }

        resultValue = null;
        return false;
    }

    /// <summary>
    /// Tries to serialize the .net runtime representation to the
    /// serialized result representation.
    /// </summary>
    /// <param name="runtimeValue">
    /// The .net runtime representation.
    /// </param>
    /// <param name="resultValue">
    /// The serialized result value.
    /// </param>
    /// <returns>
    /// Returns the serialized result value.
    /// </returns>
    protected abstract bool TrySerialize(
        TRuntimeType runtimeValue,
        [NotNullWhen(true)] out int? resultValue);

    /// <inheritdoc />
    public override bool TryDeserialize(
        object? resultValue,
        out object? runtimeValue)
    {
        if (resultValue is null)
        {
            runtimeValue = null;
            return true;
        }

        if (resultValue is int i && TryDeserialize(i, out TRuntimeType? val))
        {
            runtimeValue = val;
            return true;
        }

        runtimeValue = null;
        return false;
    }

    /// <summary>
    /// Tries to deserializes the value from the output format to the .net
    /// runtime representation.
    /// </summary>
    /// <param name="resultValue">
    /// The serialized result value.
    /// </param>
    /// <param name="runtimeValue">
    /// The .net runtime representation.
    /// </param>
    /// <returns>
    /// <c>true</c> if the serialized value was correctly deserialized; otherwise, <c>false</c>.
    /// </returns>
    protected abstract bool TryDeserialize(
        int resultValue,
        [NotNullWhen(true)] out TRuntimeType? runtimeValue);
}
