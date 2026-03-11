using System.Text.RegularExpressions;

namespace HotChocolate.Types;

/// <summary>
/// The `PhoneNumber` scalar type represents a value that conforms to the standard
/// E.164 format. <a href="https://en.wikipedia.org/wiki/E.164">See More</a>.
/// </summary>
public partial class PhoneNumberType : RegexType
{
    /// <summary>
    /// Regex that validates the standard E.164 format
    /// </summary>
    private const string ValidationPattern = @"^\+[1-9][0-9]{2,14}\z";

    [GeneratedRegex(ValidationPattern, RegexOptions.None, DefaultRegexTimeoutInMs)]
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

    protected override LeafCoercionException FormatException(string runtimeValue)
        => ThrowHelper.PhoneNumberType_InvalidFormat(this);
}
