using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `HSL` scalar type represents a valid a CSS HSL color as defined in
/// <a href="https://www.w3.org/TR/css-color-3/#hsl-color">W3 HSL Colors</a>
/// </summary>
public partial class HslType : RegexType
{
    private const string _validationPattern =
        "^(?:hsla?)\\((?:\\d+%?(?:deg|rad|grad|turn)?(?:,|\\s)+){2,3}[\\s\\/]*[\\d\\.]+%?\\)";

    [GeneratedRegex(_validationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="HslType"/> class.
    /// </summary>
    public HslType(
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
    /// Initializes a new instance of the <see cref="HslType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public HslType()
        : this(
            WellKnownScalarTypes.Hsl,
            ScalarResources.HslType_Description)
    {
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
    {
        return ThrowHelper.HslType_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        return ThrowHelper.HslType_ParseValue_IsInvalid(this);
    }
}
