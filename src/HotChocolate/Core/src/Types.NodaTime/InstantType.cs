using System.Globalization;
using NodaTime;
using NodaTime.Text;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;

namespace HotChocolate.Types.NodaTime
{
    /// <summary>
    /// Represents an instant on the global timeline, with nanosecond resolution.
    /// </summary>
    public class InstantType : StringToStructBaseType<Instant>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InstantType"/>.
        /// </summary>
        public InstantType() : base("Instant")
        {
            Description = NodaTimeResources.InstantType_Description;
        }

        /// <inheritdoc />
        protected override string Serialize(Instant runtimeValue)
            => InstantPattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        /// <inheritdoc />
        protected override bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out Instant? runtimeValue)
            => InstantPattern.ExtendedIso
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
