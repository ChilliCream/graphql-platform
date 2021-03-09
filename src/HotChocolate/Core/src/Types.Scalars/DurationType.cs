using System.Text.RegularExpressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Scalars
{
    /// <summary>
    /// A string representing a duration conforming to the ISO8601 standard.
    /// </summary>
    /// <remarks>
    /// P is the duration designator (for period) placed at the start of the duration representation.
    /// Y is the year designator that follows the value for the number of years.
    /// M is the month designator that follows the value for the number of months.
    /// W is the week designator that follows the value for the number of weeks.
    /// D is the day designator that follows the value for the number of days.
    /// T is the time designator that precedes the time components of the representation.
    /// H is the hour designator that follows the value for the number of hours.
    /// M is the minute designator that follows the value for the number of minutes.
    /// S is the second designator that follows the value for the number of seconds.
    /// Note the time designator, T, that precedes the time value.
    /// Matches moment.js, Luxon and DateFns implementations
    /// ,/. is valid for decimal places and +/- is a valid prefix
    /// </remarks>
    public class DurationType : RegexType
    {
        private const string _validationPattern =
            "^(-|\\+)?P(?!$)((-|\\+)?\\d+(?:(\\.|,)\\d+)?Y)?((-|\\+)?\\d+(?:(\\.|,)\\d+)?M)?((-|\\+)?\\d+(?:" +
            "(\\.|,)\\d+)?W)?((-|\\+)?\\d+(?:(\\.|,)\\d+)?D)?(T(?=(-|\\+)?\\d)((-|\\+)?\\d+(?:(\\.|,)\\d+)?H)?" +
            "((-|\\+)?\\d+(?:(\\.|,)\\d+)?M)?((-|\\+)?\\d+(?:(\\.|,)\\d+)?S)?)?$";

        /// <summary>
        /// Initializes a new instance of the <see cref="DurationType"/> class.
        /// </summary>
        public DurationType()
            : base(
                WellKnownScalarTypes.Duration,
                _validationPattern,
                ScalarResources.DurationType_Description,
                RegexOptions.Compiled | RegexOptions.IgnoreCase)
        {
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseLiteralError(IValueNode valueSyntax)
        {
            return ThrowHelper.DurationType_ParseLiteral_IsInvalid(this);
        }

        /// <inheritdoc />
        protected override SerializationException CreateParseValueError(object runtimeValue)
        {
            return ThrowHelper.DurationType_ParseValue_IsInvalid(this);
        }
    }
}
