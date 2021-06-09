using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.NodaTime.Properties;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    /// <summary>
    /// Represents a period of time expressed in human chronological terms:
    /// hours, days, weeks, months and so on.
    /// </summary>
    public class PeriodType : StringToClassBaseType<Period>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PeriodType"/>.
        /// </summary>
        public PeriodType() : base("Period")
        {
            Description = NodaTimeResources.PeriodType_Description;
        }

        /// <inheritdoc />
        protected override string Serialize(Period runtimeValue)
            => PeriodPattern.Roundtrip.Format(runtimeValue);

        /// <inheritdoc />
        protected override bool TryDeserialize(
            string resultValue,
            [NotNullWhen(true)] out Period? runtimeValue)
            => PeriodPattern.Roundtrip.TryParse(resultValue, out runtimeValue);
    }
}
