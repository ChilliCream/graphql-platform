using System;
using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `MacAddess` scalar type represents a IEEE 802 48-bit Mac address, represented as UTF-8
    /// character sequences. The scalar follows the specification defined in
    /// <a href="https://tools.ietf.org/html/rfc7042#page-19">RFC7042</a>
    /// </summary>
    public class MacAddressType : RegexType
    {
        private const string _validationPattern =
            @"^(?:[0-9A-Fa-f]{2}([:-]?)[0-9A-Fa-f]{2})(?:(?:\1|\.)(?:[0-9A-Fa-f]{2}([:-]?)" +
            "[0-9A-Fa-f]{2})){2}$";

        /// <summary>
        /// Initializes a new instance of the <see cref="MacAddressType"/> class.
        /// </summary>
        public MacAddressType()
            : base(
                WellKnownScalarTypes.MacAddress,
                _validationPattern,
                ScalarResources.MacAddressType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.MacAddressType_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.MacAddressType_ParseValue_IsInvalid(this);
        }
    }
}
