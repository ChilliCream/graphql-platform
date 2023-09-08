using System;
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
#if NET7_0_OR_GREATER
public partial class EmailAddressType : RegexType
#else
public class EmailAddressType : RegexType
#endif
{
    private const string _validationPattern =
        "^[a-zA-Z0-9.!#$%&'*+\\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?" +
        "(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";

#if NET7_0_OR_GREATER
    [GeneratedRegex(_validationPattern, RegexOptions.IgnoreCase, DefaultRegexTimeoutInMs)]
    private static partial Regex CreateRegex();
#else
    private static Regex CreateRegex()
        => new Regex(
            _validationPattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(DefaultRegexTimeoutInMs));
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailAddressType"/> class.
    /// </summary>
    public EmailAddressType()
        : this(
            WellKnownScalarTypes.EmailAddress,
            ScalarResources.EmailAddressType_Description) { }

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

    /// <inheritdoc />
    protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        => ThrowHelper.EmailAddressType_ParseLiteral_IsInvalid(this);

    /// <inheritdoc />
    protected override SerializationException CreateParseValueError(object runtimeValue)
        => ThrowHelper.EmailAddressType_ParseValue_IsInvalid(this);
}
