using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    public class OffsetDateType : StringToStructBaseType<OffsetDate>
    {
        public OffsetDateType() : base("OffsetDate")
        {
            Description =
                "A combination of a LocalDate and an Offset, to represent a date " +
                    "at a specific offset from UTC but without any time-of-day information.";
        }

        protected override string Serialize(OffsetDate baseValue)
            => OffsetDatePattern.GeneralIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(baseValue);

        protected override bool TryDeserialize(string str, [NotNullWhen(true)] out OffsetDate? output)
            => OffsetDatePattern.GeneralIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(str, out output);
    }
}
