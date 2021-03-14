using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    public class USCurrencyType : RegexType
    {
        private const string _validationPattern =
            "(^(?:(?:(?:0?0?[0-9]|0?[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])\\.){3}(?:0?0?" +
            "[0-9]|0?[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])(?:\\/(?:[0-9]|[1-2][0-9]|3[0" +
            "-2]))?)$)";

        /// <summary>
        /// Initializes a new instance of the <see cref="IPv4Type"/> class.
        /// </summary>
        public USCurrencyType()
            : base(
                WellKnownScalarTypes.USCurrency,
                _validationPattern,
                ScalarResources.USCurrencyType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.USCurrencyType_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.USCurrencyType_ParseValue_IsInvalid(this);
        }
    }
}
