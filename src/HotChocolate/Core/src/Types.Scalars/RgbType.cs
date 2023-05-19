using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `Rgb` scalar type represents a valid CSS RGB color
/// <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()">
/// MDN CSS Color
/// </a>
/// </summary>
#if NET7_0_OR_GREATER
public partial class RgbType : RegexType
#else
public class RgbType : RegexType
#endif
{
    private const string _validationPattern =
        "((?:rgba?)\\((?:\\d+%?(?:,|\\s)+){2,3}[\\s\\/]*[\\d\\.]+%?\\))";

#if NET7_0_OR_GREATER
    [GeneratedRegex(_validationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();
#else
    private static Regex CreateRegex()
        => new Regex(
            _validationPattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(DefaultRegexTimeoutInMs));
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="IPv6Type"/> class.
    /// </summary>
    public RgbType()
        : this(
            WellKnownScalarTypes.Rgb,
            ScalarResources.RgbType_Description)
    {
    }

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
