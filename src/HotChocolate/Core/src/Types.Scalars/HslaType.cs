using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `HSLA` scalar type represents a valid a CSS HSLA color as defined
/// in <a href="https://www.w3.org/TR/css-color-3/#hsla-color">W3 HSLA Color</a>
/// </summary>
public partial class HslaType : RegexType
{
    private const string ValidationPattern =
        "^(?:hsla?)\\((?:[0-9]+%?(?:deg|rad|grad|turn)?(?:,|\\s)+){2,3}[\\s\\/]*[0-9\\.]+%?\\)";

    [GeneratedRegex(ValidationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();

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

    /// <summary>
    /// Initializes a new instance of the <see cref="HslaType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public HslaType()
        : this(
            WellKnownScalarTypes.Hsla,
            ScalarResources.HslaType_Description)
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
