using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    public class LocalDateTimeType : StringToStructBaseType<LocalDateTime>
    {
        public LocalDateTimeType() : base("LocalDateTime")
        {
            Description = "A date and time in a particular calendar system.";
        }

        protected override string Serialize(LocalDateTime runtimeValue)
            => LocalDateTimePattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        protected override bool TryDeserialize(string resultValue, [NotNullWhen(true)] out LocalDateTime? runtimeValue)
            => LocalDateTimePattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
