using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    /// <summary>
    /// A LocalDateTime in a specific time zone and with a particular offset to distinguish between
    /// otherwise-ambiguous instants.\nA ZonedDateTime is global, in that it maps to
    /// a single Instant.
    /// </summary>
    public class ZonedDateTimeType : StringToStructBaseType<ZonedDateTime>
    {
        private static readonly string formatString = "uuuu'-'MM'-'dd'T'HH':'mm':'ss' 'z' 'o<g>";

        /// <summary>
        /// Initializes a new instance of <see cref="ZonedDateTimeType"/>.
        /// </summary>
        public ZonedDateTimeType()
            : base("ZonedDateTime")
        {
            Description = NodaTimeResources.ZonedDateTimeType_Description;
        }

        /// <inheritdoc />
        protected override string Serialize(ZonedDateTime runtimeValue)
            => ZonedDateTimePattern
                .CreateWithInvariantCulture(formatString, DateTimeZoneProviders.Tzdb)
                .Format(runtimeValue);

        /// <inheritdoc />
        protected override bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out ZonedDateTime? runtimeValue)
            => ZonedDateTimePattern
                .CreateWithInvariantCulture(formatString, DateTimeZoneProviders.Tzdb)
                .TryParse(resultValue, out runtimeValue);
    }
}
