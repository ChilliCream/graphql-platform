using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
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
    protected override TRuntimeType OnCoerceInputLiteral(IntValueNode valueLiteral)
    {
        if (TryCoerceRuntimeValue(valueLiteral.ToInt32(), out var runtimeValue))
        {
            return runtimeValue.Value;
        }

        throw new LeafCoercionException(
            string.Format(IntToStructBaseType_ParseLiteral_UnableToDeserializeInt, Name),
            this);
    }

    /// <inheritdoc />
    protected override TRuntimeType OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryCoerceRuntimeValue(inputValue.GetInt32(), out var runtimeValue))
        {
            return runtimeValue.Value;
        }

        throw new LeafCoercionException(
            string.Format(IntToStructBaseType_ParseLiteral_UnableToDeserializeInt, Name),
            this);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(TRuntimeType runtimeValue, ResultElement resultValue)
    {
        if (TryCoerceOutputValue(runtimeValue, out var value))
        {
            resultValue.SetNumberValue(value.Value);
            return;
        }

        throw new LeafCoercionException(
            string.Format(IntToStructBaseType_ParseLiteral_UnableToCoerceOutputValue, Name),
            this);
    }

    /// <inheritdoc />
    protected override IntValueNode OnValueToLiteral(TRuntimeType runtimeValue)
    {
        if (TryCoerceOutputValue(runtimeValue, out var value))
        {
            return new IntValueNode(value.Value);
        }

        throw new LeafCoercionException(
            string.Format(IntToStructBaseType_ParseLiteral_UnableToCoerceOutputValue, Name),
            this);
    }

    /// <summary>
    /// Attempts to coerce an integer input value to the runtime type.
    /// </summary>
    /// <param name="resultValue">
    /// The integer value to coerce.
    /// </param>
    /// <param name="runtimeValue">
    /// When this method returns, contains the coerced runtime value if the conversion succeeded,
    /// or the default value if the conversion failed.
    /// </param>
    /// <returns>
    /// <c>true</c> if the coercion was successful; otherwise, <c>false</c>.
    /// </returns>
    protected abstract bool TryCoerceRuntimeValue(
        int resultValue,
        [NotNullWhen(true)] out TRuntimeType? runtimeValue);

    /// <summary>
    /// Attempts to coerce a runtime value to an integer output value.
    /// </summary>
    /// <param name="runtimeValue">
    /// The runtime value to coerce.
    /// </param>
    /// <param name="resultValue">
    /// When this method returns, contains the coerced integer value if the conversion succeeded,
    /// or null if the conversion failed.
    /// </param>
    /// <returns>
    /// <c>true</c> if the coercion was successful; otherwise, <c>false</c>.
    /// </returns>
    protected abstract bool TryCoerceOutputValue(
        TRuntimeType runtimeValue,
        [NotNullWhen(true)] out int? resultValue);
}
