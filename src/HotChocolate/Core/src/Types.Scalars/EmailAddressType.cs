using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `EmailAddress` scalar type constitutes a valid email address, represented as a UTF-8
    /// character sequence. The scalar follows the specification defined by the
    /// <a href="https://html.spec.whatwg.org/multipage/input.html#valid-e-mail-address">
    /// HTML Spec
    /// </a>
    /// </summary>
    public class EmailAddressType : RegexType
    {
        private const string _validationPattern =
            "^[a-zA-Z0-9.!#$%&'*+\\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?" +
            "(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAddressType"/> class.
        /// </summary>
        public EmailAddressType()
            : base(
                WellKnownScalarTypes.EmailAddress,
                _validationPattern,
                ScalarResources.EmailAddressType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.EmailAddressType_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.EmailAddressType_ParseValue_IsInvalid(this);
        }
    }
}
