using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    public class LocalDateType : StringToStructBaseType<LocalDate>
    {
        public LocalDateType() : base("LocalDate")
        {
            Description =
                "LocalDate is an immutable struct representing a date " +
                    "within the calendar, with no reference to a particular " +
                    "time zone or time of day.";
        }

        protected override string Serialize(LocalDate runtimeValue)
            => LocalDatePattern.Iso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        protected override bool TryDeserialize(string resultValue, [NotNullWhen(true)] out LocalDate? runtimeValue)
            => LocalDatePattern.Iso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
