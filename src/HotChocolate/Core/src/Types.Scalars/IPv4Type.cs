using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `IPv4` scalar type represents a valid a IPv4 address as defined in
    /// <a href="https://tools.ietf.org/html/rfc791">RFC791</a>
    /// </summary>
    public class IPv4Type : RegexType
    {
        private const string _validationPattern =
            "(^(?:(?:(?:0?0?[0-9]|0?[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])\\.){3}(?:0?0?" +
            "[0-9]|0?[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])(?:\\/(?:[0-9]|[1-2][0-9]|3[0" +
            "-2]))?)$)";

        /// <summary>
        /// Initializes a new instance of the <see cref="IPv4Type"/> class.
        /// </summary>
        public IPv4Type()
            : base(
                WellKnownScalarTypes.IPv4,
                _validationPattern,
                ScalarResources.IPv4Type_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.IPv4Type_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.IPv4Type_ParseValue_IsInvalid(this);
        }
    }
}
