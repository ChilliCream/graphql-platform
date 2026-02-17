using System.Text.RegularExpressions;

namespace HotChocolate.Types;

/// <summary>
/// The `HSLA` scalar type represents a valid a CSS HSLA color as defined
/// in <a href="https://www.w3.org/TR/css-color-3/#hsla-color">W3 HSLA Color</a>
/// </summary>
public partial class HslaType : RegexType
{
    private const string ValidationPattern =
        "^(?:hsla?)\\((?:[0-9]+%?(?:deg|rad|grad|turn)?(?:,|\\s)+){2}[0-9]+%?\\s*[,\\/]\\s*[0-9\\.]+%?\\)";

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

    protected override LeafCoercionException FormatException(string runtimeValue)
        => ThrowHelper.HslaType_InvalidFormat(this);
}
