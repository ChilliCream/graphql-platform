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

        protected override bool TrySerialize(IsoDayOfWeek runtimeValue, [NotNullWhen(true)] out int? resultValue)
        {
            if (runtimeValue == IsoDayOfWeek.None)
            {
                resultValue = null;
                return false;
            }

            resultValue = (int)runtimeValue;
            return true;
        }

        protected override bool TryDeserialize(int resultValue, [NotNullWhen(true)] out IsoDayOfWeek? runtimeValue)
        {
            if (resultValue < 1 || resultValue > 7)
            {
                runtimeValue = null;
                return false;
            }

            runtimeValue = (IsoDayOfWeek)resultValue;
            return true;
        }
    }
}