using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `HexColor` scalar type represents a valid HEX color code as defined in
/// <a href="https://www.w3.org/TR/css-color-4/#hex-notation">W3 HEX notation</a>
/// </summary>
#if NET7_0_OR_GREATER
public partial class HexColorType : RegexType
#else
public class HexColorType : RegexType
#endif
{
    private const string _validationPattern =
        "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3}|[A-Fa-f0-9]{8})$";

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
    /// Initializes a new instance of the <see cref="HexColorType"/> class.
    /// </summary>
    public HexColorType()
        : this(
            WellKnownScalarTypes.HexColor,
            ScalarResources.HexColorType_Description)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HexColorType"/> class.
    /// </summary>
    public HexColorType(
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
        return ThrowHelper.HexColorType_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        return ThrowHelper.HexColorType_ParseValue_IsInvalid(this);
    }
}
