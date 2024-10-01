using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `ISBN` scalar type is a ISBN-10 or ISBN-13 number:
/// <a>https://en.wikipedia.org/wiki/International_Standard_Book_Number</a>.
/// </summary>
public partial class IsbnType : RegexType
{
    private const string _validationPattern =
        "^(?:ISBN(-1(?:(0)|3))?:?\\ )?(?(1)(?(2)(?=[0-9X]{10}$|(?=(?:[0-9]+[- ]){3})[- 0" +
        "-9X]{13}$)[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9X]|(?=[0-9]{13}$|(?=(?:[0-9]" +
        "+[- ]){4})[- 0-9]{17}$)97[89][- ]?[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9])|(" +
        "?=[0-9X]{10}$|(?=(?:[0-9]+[- ]){3})[- 0-9X]{13}$|97[89][0-9]{10}$|(?=(?:[0-9]+[" +
        "- ]){4})[- 0-9]{17}$)(?:97[89][- ]?)?[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9X" +
        "])$";

    [GeneratedRegex(_validationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
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

    /// <inheritdoc />
    protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
    {
        return ThrowHelper.IsbnType_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        return ThrowHelper.IsbnType_ParseValue_IsInvalid(this);
    }
}
