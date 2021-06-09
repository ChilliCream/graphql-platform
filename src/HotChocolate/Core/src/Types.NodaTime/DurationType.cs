using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    public class DurationType : StringToStructBaseType<Duration>
    {
        public DurationType() : base("Duration")
        {
            Description = "Represents a fixed (and calendar-independent) length of time.";
        }

        protected override string Serialize(Duration runtimeValue)
            => DurationPattern.Roundtrip
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        protected override bool TryDeserialize(string resultValue, [NotNullWhen(true)] out Duration? runtimeValue)
            => DurationPattern.Roundtrip
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
