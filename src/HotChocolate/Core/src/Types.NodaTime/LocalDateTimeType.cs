using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    /// <summary>
    /// A date and time in a particular calendar system.
    /// </summary>
    public class LocalDateTimeType : StringToStructBaseType<LocalDateTime>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="LocalDateTimeType"/>.
        /// </summary>
        public LocalDateTimeType() : base("LocalDateTime")
        {
            Description = NodaTimeResources.LocalDateTimeType_Description;
        }

        /// <inheritdoc />
        protected override string Serialize(LocalDateTime runtimeValue)
            => LocalDateTimePattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        /// <inheritdoc />
        protected override bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out LocalDateTime? runtimeValue)
            => LocalDateTimePattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
