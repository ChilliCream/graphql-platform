using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `Rgba` scalar type represents a valid CSS RGBA color
/// <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/color_value#rgb()_and_rgba()"></a>
/// </summary>
public partial class RgbaType : RegexType
{
    private const string ValidationPattern =
        "rgba?\\((?:[0-9]+%?(?:,|\\s)+){2}[0-9]+%?\\s*[,\\/]\\s*[0-9\\.]+%?\\)";

    [GeneratedRegex(ValidationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
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

    protected override LeafCoercionException FormatException(string runtimeValue)
        => ThrowHelper.RgbaType_InvalidFormat(this);
}
