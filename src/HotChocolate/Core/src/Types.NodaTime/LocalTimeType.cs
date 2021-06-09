using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    public class LocalTimeType : StringToStructBaseType<LocalTime>
    {
        public LocalTimeType() : base("LocalTime")
        {
            Description = "LocalTime is an immutable struct representing a time of day, with no reference to a particular calendar, time zone or date.";
        }


        protected override string Serialize(LocalTime runtimeValue)
            => LocalTimePattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        protected override bool TryDeserialize(string resultValue, [NotNullWhen(true)] out LocalTime? runtimeValue)
            => LocalTimePattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
