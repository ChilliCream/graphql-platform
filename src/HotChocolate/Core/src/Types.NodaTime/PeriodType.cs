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

        protected override string Serialize(Period baseValue)
            => PeriodPattern.Roundtrip
                .Format(baseValue);

        protected override bool TryDeserialize(string str, [NotNullWhen(true)] out Period? output)
            => PeriodPattern.Roundtrip
                .TryParse(str, out output);
    }
}
