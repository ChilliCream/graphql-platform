using System.Diagnostics.CodeAnalysis;
using NodaTime;
using NodaTime.Text;

namespace HotChocolate.Types.NodaTime
{
    public class PeriodType : StringToClassBaseType<Period>
    {
        public PeriodType() : base("Period")
        {
            Description =
                "Represents a period of time expressed in human chronological " +
                    "terms: hours, days, weeks, months and so on.";
        }

        protected override string Serialize(Period runtimeValue)
            => PeriodPattern.Roundtrip
                .Format(runtimeValue);

        protected override bool TryDeserialize(string resultValue, [NotNullWhen(true)] out Period? runtimeValue)
            => PeriodPattern.Roundtrip
                .TryParse(resultValue, out runtimeValue);
    }
}
