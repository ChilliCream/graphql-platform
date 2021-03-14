using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// The `UtcOffset` scalar type represents a field whose value is a UTC Offset
    /// <a>https://en.wikipedia.org/wiki/List_of_tz_database_time_zones</a>
    /// </summary>
    public class UtcOffsetType : RegexType
    {
        private const string _validationPattern = "^([+-]?)(\\d{2}):(\\d{2})$";

        /// <summary>
        /// Initializes a new instance of the <see cref="UtcOffsetType"/> class.
        /// </summary>
        public UtcOffsetType()
            : base(
                WellKnownScalarTypes.UtcOffset,
                _validationPattern,
                ScalarResources.UtcOffsetType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.UtcOffsetType_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.UtcOffsetType_ParseValue_IsInvalid(this);
        }
    }
}
