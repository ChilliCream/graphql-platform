using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `Rgb` scalar type represents a valid CSS RGB color
/// <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()">
/// MDN CSS Color
/// </a>
/// </summary>
public partial class RgbType : RegexType
{
    private const string _validationPattern =
        "((?:rgba?)\\((?:\\d+%?(?:,|\\s)+){2,3}[\\s\\/]*[\\d\\.]+%?\\))";

    [GeneratedRegex(_validationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbType"/> class.
    /// </summary>
    public RgbType(
        string name,
        string? description = null,
        BindingBehavior bind = BindingBehavior.Explicit)
        : base(
            name,
            CreateRegex(),
            description,
            bind)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IPv6Type"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public RgbType()
        : this(
            WellKnownScalarTypes.Rgb,
            ScalarResources.RgbType_Description)
    {
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
    {
        return ThrowHelper.RgbType_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        return ThrowHelper.RgbType_ParseValue_IsInvalid(this);
    }
}
