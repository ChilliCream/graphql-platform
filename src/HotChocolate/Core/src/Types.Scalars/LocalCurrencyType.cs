using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;

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
    public override IValueNode ParseResult(object? resultValue)
    {
        return resultValue switch
        {
            null => NullValueNode.Default,
            string s => new StringValueNode(s),
            decimal d => ParseValue(d),
            _ => throw ThrowHelper.LocalCurrencyType_ParseValue_IsInvalid(this),
        };
    }

    /// <inheritdoc />
    protected override decimal ParseLiteral(StringValueNode valueSyntax)
    {
        if (TryDeserializeFromString(valueSyntax.Value, out var value))
        {
            return value.Value;
        }

        throw ThrowHelper.LocalCurrencyType_ParseLiteral_IsInvalid(this);
    }

    protected override StringValueNode ParseValue(decimal runtimeValue)
    {
        return new(Serialize(runtimeValue));
    }

    /// <inheritdoc />
    public override bool TrySerialize(object? runtimeValue, out object? resultValue)
    {
        switch (runtimeValue)
        {
            case null:
                resultValue = null;
                return true;
            case decimal d:
                resultValue = Serialize(d);
                return true;
            default:
                resultValue = null;
                return false;
        }
    }

    /// <inheritdoc />
    public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
    {
        switch (resultValue)
        {
            case null:
                runtimeValue = null;
                return true;
            case string s when TryDeserializeFromString(s, out var d):
                runtimeValue = d;
                return true;
            case decimal d:
                runtimeValue = d;
                return true;
            default:
                runtimeValue = null;
                return false;
        }
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
