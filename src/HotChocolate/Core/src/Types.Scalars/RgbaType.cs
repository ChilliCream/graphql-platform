using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `Rgba` scalar type represents a valid CSS RGBA color
/// <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()"></a>
/// </summary>
public partial class RgbaType : RegexType
{
    private const string _validationPattern =
        "((?:rgba?)\\((?:[0-9]+%?(?:,|\\s)+){2,3}[\\s\\/]*[0-9\\.]+%?\\))";

    [GeneratedRegex(_validationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();

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

    /// <summary>
    /// Initializes a new instance of the <see cref="RgbaType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public RgbaType()
        : this(
            WellKnownScalarTypes.Rgba,
            ScalarResources.RgbaType_Description)
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
