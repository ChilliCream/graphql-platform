using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    /// <summary>
    /// Represents a fixed (and calendar-independent) length of time.
    /// </summary>
    public class DurationType : StringToStructBaseType<Duration>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DurationType"/>.
        /// </summary>
        public DurationType() : base("Duration")
        {
            Description = NodaTimeResources.DurationType_Description;
        }

        /// <inheritdoc />
        protected override string Serialize(Duration runtimeValue)
            => DurationPattern.Roundtrip
                .WithCulture(CultureInfo.InvariantCulture)
                .Format(runtimeValue);

        /// <inheritdoc />
        protected override bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out Duration? runtimeValue)
            => DurationPattern.Roundtrip
                .WithCulture(CultureInfo.InvariantCulture)
                .TryParse(resultValue, out runtimeValue);
    }
}
