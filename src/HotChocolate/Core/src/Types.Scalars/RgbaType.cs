using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `Rgba` scalar type represents a valid CSS RGBA color
/// <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()"></a>
/// </summary>
#if NET7_0_OR_GREATER
public partial class RgbaType : RegexType
#else
public class RgbaType : RegexType
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
    /// Initializes a new instance of the <see cref="RgbaType"/> class.
    /// </summary>
    public RgbaType()
        : this(
            WellKnownScalarTypes.Rgba,
            ScalarResources.RgbaType_Description)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaType"/> class.
    /// </summary>
    public RgbaType(
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
        return ThrowHelper.RgbaType_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        return ThrowHelper.RgbaType_ParseValue_IsInvalid(this);
    }
}
