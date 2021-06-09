using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    /// <summary>
    /// A combination of a LocalTime and an Offset, to represent a time-of-day at a specific offset
    /// from UTC but without any date information.
    /// </summary>
    public class OffsetTimeType : StringToStructBaseType<OffsetTime>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="OffsetTimeType"/>.
        /// </summary>
        public OffsetTimeType() : base("OffsetTime")
        {
            Description = NodaTimeResources.OffsetTimeType_Description;
        }

        /// <inheritdoc />
        protected override string Serialize(OffsetTime runtimeValue)
            => OffsetTimePattern.GeneralIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        /// <inheritdoc />
        protected override bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out OffsetTime? runtimeValue)
            => OffsetTimePattern.GeneralIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
