using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `HSLA` scalar type represents a valid a CSS HSLA color as defined
/// in <a href="https://www.w3.org/TR/css-color-3/#hsla-color">W3 HSLA Color</a>
/// </summary>
#if NET7_0_OR_GREATER
public partial class HslaType : RegexType
#else
public class HslaType : RegexType
#endif
{
    private const string _validationPattern =
        "^(?:hsla?)\\((?:\\d+%?(?:deg|rad|grad|turn)?(?:,|\\s)+){2,3}[\\s\\/]*[\\d\\.]+%?\\)";

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
    /// Initializes a new instance of the <see cref="HslaType"/> class.
    /// </summary>
    public HslaType()
        : this(
            WellKnownScalarTypes.Hsla,
            ScalarResources.HslaType_Description)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HslaType"/> class.
    /// </summary>
    public HslaType(
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
        return ThrowHelper.HslaType_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        return ThrowHelper.HslaType_ParseValue_IsInvalid(this);
    }
}
