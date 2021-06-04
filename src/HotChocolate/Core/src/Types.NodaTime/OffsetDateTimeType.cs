using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    public class OffsetDateTimeType : StringToStructBaseType<OffsetDateTime>
    {
        public OffsetDateTimeType() : base("OffsetDateTime")
        {
            Description = "A local date and time in a particular calendar system, combined with an offset from UTC.";
        }

        protected override string Serialize(OffsetDateTime baseValue)
            => OffsetDateTimePattern.GeneralIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(baseValue);

        protected override bool TryDeserialize(string str, [NotNullWhen(true)] out OffsetDateTime? output)
            => OffsetDateTimePattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(str, out output);
    }
}
