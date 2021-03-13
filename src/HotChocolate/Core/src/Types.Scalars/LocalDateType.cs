using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `LocalDate` scalar type represents a ISO date part, represented as UTF-8
    /// character sequences YYYY-MM-DD. The scalar follows the specification defined in
    /// <a href="https://tools.ietf.org/html/rfc3339">RFC3339</a>
    /// </summary>
    public class LocalDateType : RegexType
    {
        private const string _validationPattern =
            @"^[0-9]{4}-(((0[13578]|(10|12))-(0[1-9]|[1-2][0-9]|3[0-1]))|(02-(0" +
            "[1-9]|[1-2][0-9]))|((0[469]|11)-(0[1-9]|[1-2][0-9]|30)))$";

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalDateType"/> class.
        /// </summary>
        public LocalDateType()
            : base(
                WellKnownScalarTypes.LocalDate,
                _validationPattern,
                ScalarResources.LocalDateType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.LocalDateType_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.LocalDateType_ParseValue_IsInvalid(this);
        }
    }
}
