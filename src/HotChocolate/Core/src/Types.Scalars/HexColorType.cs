using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `HexColor` scalar type represents a valid HEX color code as defined in
/// <a href="https://www.w3.org/TR/css-color-4/#hex-notation">W3 HEX notation</a>
/// </summary>
public partial class HexColorType : RegexType
{
    private const string _validationPattern =
        "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3}|[A-Fa-f0-9]{8})$";

    [GeneratedRegex(_validationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();

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

    /// <summary>
    /// Initializes a new instance of the <see cref="HexColorType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public HexColorType()
        : this(
            WellKnownScalarTypes.HexColor,
            ScalarResources.HexColorType_Description)
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
