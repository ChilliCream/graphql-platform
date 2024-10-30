using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `PhoneNumber` scalar type scalar type represents a value that conforms to the standard
/// E.164 format. <a href="https://en.wikipedia.org/wiki/E.164">See More</a>.
/// </summary>
public partial class PhoneNumberType : RegexType
{
    /// <summary>
    /// Regex that validates the standard E.164 format
    /// </summary>
    private const string _validationPattern = "^\\+[1-9]\\d{1,14}$";

    [GeneratedRegex(_validationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="PhoneNumberType"/>
    /// </summary>
    public PhoneNumberType(
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
    /// Initializes a new instance of the <see cref="PhoneNumberType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public PhoneNumberType()
        : this(
            WellKnownScalarTypes.PhoneNumber,
            ScalarResources.PhoneNumberType_Description)
    {
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
    {
        return ThrowHelper.PhoneNumber_ParseLiteral_IsInvalid(this);
    }

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
    {
        return ThrowHelper.PhoneNumber_ParseValue_IsInvalid(this);
    }
}
