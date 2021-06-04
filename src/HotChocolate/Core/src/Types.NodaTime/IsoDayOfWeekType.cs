using System.Diagnostics.CodeAnalysis;
using NodaTime;

namespace HotChocolate.Types.NodaTime
{
    public class IsoDayOfWeekType : IntToStructBaseType<IsoDayOfWeek>
    {
        public IsoDayOfWeekType() : base("IsoDayOfWeek")
        {
            Description =
                "Equates the days of the week with their numerical value according to ISO-8601.\n" +
                "Monday = 1, Tuesday = 2, Wednesday = 3, Thursday = 4, Friday = 5, Saturday = 6, Sunday = 7.";
        }

        protected override bool TrySerialize(IsoDayOfWeek baseValue, [NotNullWhen(true)] out int? output)
        {
            if (baseValue == IsoDayOfWeek.None)
            {
                output = null;
                return false;
            }

            output = (int)baseValue;
            return true;
        }

        protected override bool TryDeserialize(int val, [NotNullWhen(true)] out IsoDayOfWeek? output)
        {
            if (val < 1 || val > 7)
            {
                output = null;
                return false;
            }

            output = (IsoDayOfWeek)val;
            return true;
        }
    }
}