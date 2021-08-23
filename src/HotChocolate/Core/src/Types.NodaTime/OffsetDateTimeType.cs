using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    /// <summary>
    /// A local date and time in a particular calendar system, combined with an offset from UTC.
    /// </summary>
    public class OffsetDateTimeType : StringToStructBaseType<OffsetDateTime>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="OffsetDateTimeType"/>.
        /// </summary>
        public OffsetDateTimeType() : base("OffsetDateTime")
        {
            Description = NodaTimeResources.OffsetDateTimeType_Description;
        }

        /// <inheritdoc />
        protected override string Serialize(OffsetDateTime runtimeValue)
            => OffsetDateTimePattern.GeneralIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        /// <inheritdoc />
        protected override bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out OffsetDateTime? runtimeValue)
            => OffsetDateTimePattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
