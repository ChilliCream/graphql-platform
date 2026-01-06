using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;

namespace HotChocolate.Types;

/// <summary>
/// The Regular Expression scalar type represents textual data, represented as UTF‐8 character
/// sequences following a pattern defined as a <see cref="Regex"/>
/// </summary>
public class RegexType : ScalarType<string, StringValueNode>
{
    protected internal const int DefaultRegexTimeoutInMs = 200;

    private readonly Regex _validationRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexType"/> class.
    /// </summary>
    public RegexType(
        string name,
        string pattern,
        string? description = null,
        RegexOptions regexOptions = RegexOptions.Compiled,
        BindingBehavior bind = BindingBehavior.Explicit)
        : this(
            name,
            new Regex(
                pattern,
                regexOptions,
                TimeSpan.FromMilliseconds(DefaultRegexTimeoutInMs)),
            description,
            bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexType"/> class.
    /// </summary>
    public RegexType(
        string name,
        Regex regex,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
        _validationRegex = regex;
        Description = description;
    }

    /// <inheritdoc />
    public override bool IsInstanceOfType(object runtimeValue)
        => runtimeValue is string s && _validationRegex.IsMatch(s);

    public override bool IsValueCompatible(IValueNode valueLiteral)
        => valueLiteral is StringValueNode stringLiteral && _validationRegex.IsMatch(stringLiteral.Value);

    public override bool IsValueCompatible(JsonElement inputValue)
        => inputValue.ValueKind is JsonValueKind.String && _validationRegex.IsMatch(inputValue.GetString()!);

    protected override string OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        if (_validationRegex.IsMatch(valueLiteral.Value))
        {
            return valueLiteral.Value;
        }

        throw FormatException();
    }

    protected override string OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        var runtimeValue = inputValue.GetString()!;

        if (_validationRegex.IsMatch(runtimeValue))
        {
            return runtimeValue;
        }

        throw FormatException();
    }

    protected override void OnCoerceOutputValue(string runtimeValue, ResultElement resultValue)
    {
        if (_validationRegex.IsMatch(runtimeValue))
        {
            resultValue.SetStringValue(runtimeValue);
            return;
        }

        throw FormatException();
    }

    protected override StringValueNode OnValueToLiteral(string runtimeValue)
    {
        if (_validationRegex.IsMatch(runtimeValue))
        {
            return new StringValueNode(runtimeValue);
        }

        throw FormatException();
    }

    protected virtual LeafCoercionException FormatException()
        => ThrowHelper.RegexType_InvalidFormat(this, Name);
}
