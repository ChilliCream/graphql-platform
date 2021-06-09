using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    /// <summary>
    /// LocalDate is an immutable struct representing a date within the calendar,
    /// with no reference to a particular time zone or time of day.
    /// </summary>
    public class LocalDateType : StringToStructBaseType<LocalDate>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LocalDateType"/>.
        /// </summary>
        public LocalDateType() : base("LocalDate")
        {
            Description = NodaTimeResources.LocalDateType_Description;
        }

        /// <inheritdoc />
        protected override string Serialize(LocalDate runtimeValue)
            => LocalDatePattern.Iso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        /// <inheritdoc />
        protected override bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out LocalDate? runtimeValue)
            => LocalDatePattern.Iso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
