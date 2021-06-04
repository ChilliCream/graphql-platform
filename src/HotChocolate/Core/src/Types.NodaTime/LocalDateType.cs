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

        protected override string Serialize(LocalDate baseValue)
            => LocalDatePattern.Iso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(baseValue);

        protected override bool TryDeserialize(string str, [NotNullWhen(true)] out LocalDate? output)
            => LocalDatePattern.Iso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(str, out output);
    }
}
