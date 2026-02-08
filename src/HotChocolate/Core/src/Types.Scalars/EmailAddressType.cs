using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types;

/// <summary>
/// The `EmailAddress` scalar type constitutes a valid email address, represented as a UTF-8
/// character sequence. The scalar follows the specification defined by the
/// <a href="https://html.spec.whatwg.org/multipage/input.html#valid-e-mail-address">
/// HTML Spec
/// </a>
/// </summary>
public partial class EmailAddressType : RegexType
{
    private const string ValidationPattern =
        @"^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?"
        + @"(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*\z";

    [GeneratedRegex(ValidationPattern, RegexOptions.None, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailAddressType"/> class.
    /// </summary>
    public EmailAddressType(
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
    /// Initializes a new instance of the <see cref="EmailAddressType"/> class.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public EmailAddressType()
        : this(
            WellKnownScalarTypes.EmailAddress,
            ScalarResources.EmailAddressType_Description)
    { }

    protected override LeafCoercionException FormatException(string runtimeValue)
        => ThrowHelper.EmailAddressType_InvalidFormat(this);
}
