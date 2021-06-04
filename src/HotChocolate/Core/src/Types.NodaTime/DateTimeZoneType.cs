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

        protected override string Serialize(DateTimeZone val)
            => val.Id;

        protected override bool TryDeserialize(string str, [NotNullWhen(true)] out DateTimeZone? output)
        {
            DateTimeZone? result = DateTimeZoneProviders.Tzdb.GetZoneOrNull(str);
            if (result == null)
            {
                output = null;
                return false;
            }
            else
            {
                output = result;
                return true;
            }
        }
    }
}
