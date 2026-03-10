using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Types.NodaTime.Properties.NodaTimeResources;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// This base class provides serialization functionality for noda time scalars
/// that have a <see cref="string"/> result value and a class runtime value.
/// </summary>
/// <typeparam name="TRuntimeType">
/// The runtime type.
/// </typeparam>
public abstract class StringToClassBaseType<TRuntimeType>
    : ScalarType<TRuntimeType, StringValueNode>
    where TRuntimeType : class
{
    /// <summary>
    /// Initializes a new instance of <see cref="StringToClassBaseType{TRuntimeType}"/>.
    /// </summary>
    /// <param name="name">
    /// The name of the scalar.
    /// </param>
    protected StringToClassBaseType(string name)
        : base(name, BindingBehavior.Implicit)
    {
    }

    /// <inheritdoc />
    protected override TRuntimeType OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryCoerceRuntimeValue(valueLiteral.Value, out var runtimeValue))
        {
            return runtimeValue;
        }

        throw new LeafCoercionException(
            string.Format(StringToClassBaseType_ParseLiteral_UnableToDeserializeString, Name),
            this);
    }

    /// <inheritdoc />
    protected override TRuntimeType OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryCoerceRuntimeValue(inputValue.GetString()!, out var runtimeValue))
        {
            return runtimeValue;
        }

        throw new LeafCoercionException(
            string.Format(StringToClassBaseType_ParseLiteral_UnableToDeserializeString, Name),
            this);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(TRuntimeType runtimeValue, ResultElement resultValue)
    {
        if (TryCoerceOutputValue(runtimeValue, out var value))
        {
            resultValue.SetStringValue(value);
            return;
        }

        throw new LeafCoercionException(
            string.Format(StringToClassBaseType_ParseLiteral_UnableToCoerceOutputValue, Name),
            this);
    }

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(TRuntimeType runtimeValue)
    {
        if (TryCoerceOutputValue(runtimeValue, out var value))
        {
            return new StringValueNode(value);
        }

        throw new LeafCoercionException(
            string.Format(StringToClassBaseType_ParseLiteral_UnableToCoerceOutputValue, Name),
            this);
    }

    /// <summary>
    /// Attempts to coerce a string input value to the runtime type.
    /// </summary>
    /// <param name="resultValue">
    /// The string value to coerce.
    /// </param>
    /// <param name="runtimeValue">
    /// When this method returns, contains the coerced runtime value if the conversion succeeded,
    /// or null if the conversion failed.
    /// </param>
    /// <returns>
    /// <c>true</c> if the coercion was successful; otherwise, <c>false</c>.
    /// </returns>
    protected abstract bool TryCoerceRuntimeValue(
        string resultValue,
        [NotNullWhen(true)] out TRuntimeType? runtimeValue);

    /// <summary>
    /// Attempts to coerce a runtime value to a string output value.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value to coerce.
    /// </param>
    /// <param name="resultValue">
    /// When this method returns, contains the coerced string value if the conversion succeeded,
    /// or null if the conversion failed.
    /// </param>
    /// <returns>
    /// <c>true</c> if the coercion was successful; otherwise, <c>false</c>.
    /// </returns>
    protected abstract bool TryCoerceOutputValue(
        TRuntimeType runtimeValue,
        [NotNullWhen(true)] out string? resultValue);
}
