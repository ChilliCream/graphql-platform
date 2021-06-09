using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    public class OffsetTimeType : StringToStructBaseType<OffsetTime>
    {
        public OffsetTimeType() : base("OffsetTime")
        {
            Description =
                "A combination of a LocalTime and an Offset, " +
                    "to represent a time-of-day at a specific offset from UTC " +
                    "but without any date information.";
        }

        protected override string Serialize(OffsetTime runtimeValue)
            => OffsetTimePattern.GeneralIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        protected override bool TryDeserialize(string resultValue, [NotNullWhen(true)] out OffsetTime? runtimeValue)
            => OffsetTimePattern.GeneralIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
