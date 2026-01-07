using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.Utilities.ThrowHelper;

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

    protected override string OnCoerceInputLiteral(StringValueNode valueLiteral)
    {
        var runtimeValue = valueLiteral.Value;
        AssertFormat(runtimeValue);
        return runtimeValue;
    }

    protected override string OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
    {
        var runtimeValue = inputValue.GetString()!;
        AssertFormat(runtimeValue);
        return runtimeValue;
    }

    protected override void OnCoerceOutputValue(string runtimeValue, ResultElement resultValue)
    {
        AssertFormat(runtimeValue);
        resultValue.SetStringValue(runtimeValue);
    }

    protected override StringValueNode OnValueToLiteral(string runtimeValue)
    {
        AssertFormat(runtimeValue);
        return new StringValueNode(runtimeValue);
    }

    private void AssertFormat(string runtimeValue)
    {
        if (!_validationRegex.IsMatch(runtimeValue))
        {
            throw FormatException(runtimeValue);
        }
    }

    protected virtual LeafCoercionException FormatException(string runtimeValue)
        => RegexType_InvalidFormat(this, Name);
}
