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
    private const string ValidationPattern =
        "rgb\\((?:[0-9]+%?(?:,|\\s)+){2}[0-9]+%?\\)";

    [GeneratedRegex(ValidationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
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
    /// Initializes a new instance of the <see cref="RgbType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public RgbType()
        : this(
            WellKnownScalarTypes.Rgb,
            ScalarResources.RgbType_Description)
    {
    }

    protected override LeafCoercionException FormatException(string runtimeValue)
        => ThrowHelper.RgbType_InvalidFormat(this);
}
