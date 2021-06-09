using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    /// <summary>
    /// A combination of a LocalDate and an Offset,
    /// to represent a date at a specific offset from UTC but
    /// without any time-of-day information.
    /// </summary>
    public class OffsetDateType : StringToStructBaseType<OffsetDate>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="OffsetDateType"/>.
        /// </summary>
        public OffsetDateType() : base("OffsetDate")
        {
            Description = NodaTimeResources.OffsetDateType_Description;
        }

        /// <inheritdoc />
        protected override string Serialize(OffsetDate runtimeValue)
            => OffsetDatePattern.GeneralIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        /// <inheritdoc />
        protected override bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out OffsetDate? runtimeValue)
            => OffsetDatePattern.GeneralIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
