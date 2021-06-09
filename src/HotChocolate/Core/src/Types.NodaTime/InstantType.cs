using System.Globalization;
using NodaTime;
using NodaTime.Text;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types.NodaTime
{
    public class InstantType : StringToStructBaseType<Instant>
    {
        public InstantType() : base("Instant")
        {
            Description = "Represents an instant on the global timeline, with nanosecond resolution.";
        }

        protected override string Serialize(Instant runtimeValue)
            => InstantPattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        protected override bool TryDeserialize(string resultValue, [NotNullWhen(true)] out Instant? runtimeValue)
            => InstantPattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
