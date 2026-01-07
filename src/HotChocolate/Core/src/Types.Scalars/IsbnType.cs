using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `ISBN` scalar type is an ISBN-10 or ISBN-13 number:
/// <a>https://en.wikipedia.org/wiki/International_Standard_Book_Number</a>.
/// </summary>
public partial class IsbnType : RegexType
{
    private const string ValidationPattern =
        "^(?:ISBN(-1(?:(0)|3))?:?\\ )?(?(1)(?(2)(?=[0-9X]{10}$|(?=(?:[0-9]+[- ]){3})[- 0"
        + "-9X]{13}$)[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9X]|(?=[0-9]{13}$|(?=(?:[0-9]"
        + "+[- ]){4})[- 0-9]{17}$)97[89][- ]?[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9])|("
        + "?=[0-9X]{10}$|(?=(?:[0-9]+[- ]){3})[- 0-9X]{13}$|97[89][0-9]{10}$|(?=(?:[0-9]+["
        + "- ]){4})[- 0-9]{17}$)(?:97[89][- ]?)?[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9X"
        + "])$";

    [GeneratedRegex(ValidationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="IsbnType"/> class.
    /// </summary>
    public IsbnType(
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
    /// Initializes a new instance of the <see cref="IsbnType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public IsbnType()
        : this(
            WellKnownScalarTypes.Isbn,
            ScalarResources.IsbnType_Description)
    {
    }

    protected override LeafCoercionException FormatException(string runtimeValue)
        => ThrowHelper.IsbnType_InvalidFormat(this);
}
