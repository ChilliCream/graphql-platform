using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The `LocalCurrency` scalar type is a currency string.
/// </summary>
public class LocalCurrencyType : ScalarType<decimal, StringValueNode>
{
    private readonly CultureInfo _cultureInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalCurrencyType"/> class.
    /// </summary>
    public LocalCurrencyType(
        string name,
        string culture = "en-US",
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(
            name,
            bind)
    {
        _cultureInfo = CultureInfo.CreateSpecificCulture(culture);
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalCurrencyType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public LocalCurrencyType()
        : this(
            WellKnownScalarTypes.LocalCurrency,
            description: ScalarResources.LocalCurrencyType_Description)
    {
    }

    /// <inheritdoc />
    protected override decimal OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (TryDeserializeFromString(valueLiteral.Value, out var value))
        {
            return value.Value;
        }

        throw ThrowHelper.LocalCurrencyType_InvalidFormat(this);
    }

    /// <inheritdoc />
    protected override decimal OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        if (TryDeserializeFromString(inputValue.GetString(), out var value))
        {
            return value.Value;
        }

        throw ThrowHelper.LocalCurrencyType_InvalidFormat(this);
    }

    /// <inheritdoc />
    protected override void OnCoerceOutputValue(decimal runtimeValue, ResultElement resultValue)
    {
        resultValue.SetStringValue(Serialize(runtimeValue));
    }

    /// <inheritdoc />
    protected override StringValueNode OnValueToLiteral(decimal runtimeValue)
    {
        return new StringValueNode(Serialize(runtimeValue));
    }

    private string Serialize(IFormattable value)
    {
        return value.ToString("c", _cultureInfo);
    }

    private bool TryDeserializeFromString(
        string? serialized,
        [NotNullWhen(true)] out decimal? value)
    {
        if (serialized is not null
            && decimal.TryParse(serialized, NumberStyles.Currency, _cultureInfo, out var d))
        {
            value = d;
            return true;
        }

        value = null;
        return false;
    }
}
