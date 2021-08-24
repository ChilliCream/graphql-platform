using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    /// <summary>
    /// LocalTime is an immutable struct representing a time of day,
    /// with no reference to a particular calendar, time zone or date.
    /// </summary>
    public class LocalTimeType : StringToStructBaseType<LocalTime>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LocalTimeType"/>.
        /// </summary>
        public LocalTimeType() : base("LocalTime")
        {
            Description = NodaTimeResources.LocalTimeType_Description;
        }

        /// <inheritdoc />
        protected override string Serialize(LocalTime runtimeValue)
            => LocalTimePattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        /// <inheritdoc />
        protected override bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out LocalTime? runtimeValue)
            => LocalTimePattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
