using System.Diagnostics.CodeAnalysis;
using NodaTime;

namespace HotChocolate.Types.NodaTime
{
    public class DateTimeZoneType : StringToClassBaseType<DateTimeZone>
    {
        public DateTimeZoneType() : base("DateTimeZone")
        {
            Description =
                "Represents a time zone - a mapping between UTC and local time.\n" +
                "A time zone maps UTC instants to local times - or, equivalently, " +
                    "to the offset from UTC at any particular instant.";
        }

        protected override string Serialize(DateTimeZone runtimeValue)
            => runtimeValue.Id;

        protected override bool TryDeserialize(string resultValue, [NotNullWhen(true)] out DateTimeZone? runtimeValue)
        {
            DateTimeZone? result = DateTimeZoneProviders.Tzdb.GetZoneOrNull(resultValue);
            if (result == null)
            {
                runtimeValue = null;
                return false;
            }
            else
            {
                runtimeValue = result;
                return true;
            }
        }
    }
}
