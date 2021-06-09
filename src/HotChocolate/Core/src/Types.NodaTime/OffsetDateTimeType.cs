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

        protected override string Serialize(OffsetDateTime runtimeValue)
            => OffsetDateTimePattern.GeneralIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        protected override bool TryDeserialize(string resultValue, [NotNullWhen(true)] out OffsetDateTime? runtimeValue)
            => OffsetDateTimePattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
