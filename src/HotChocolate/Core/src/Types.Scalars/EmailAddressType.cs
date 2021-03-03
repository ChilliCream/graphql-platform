using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `EmailAddress` scalar type represents a email address, represented as UTF-8 character
    /// sequences. The scalar follows the specification defined in RFC 5322.
    /// </summary>
    public class EmailAddressType : RegexType
    {
        /// <summary>
        /// Well established regex for email validation
        /// Source : https://emailregex.com/
        /// </summary>
        private static readonly string _validationPattern =
            "^[a-zA-Z0-9.!#$%&'*+\\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailAddressType"/> class.
        /// </summary>
        public EmailAddressType()
            : base(WellKnownScalarTypes.EmailAddress,
                _validationPattern,
                ScalarResources.EmailAddressType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        protected override Exception CreateParseLiteralError(StringValueNode valueSyntax)
        {
            return ThrowHelper.EmailAddressType_ParseLiteral_IsInvalid(this);
        }

        protected override Exception CreateParseValueError(string runtimeValue)
        {
            return ThrowHelper.EmailAddressType_ParseValue_IsInvalid(this);
        }
    }
}
