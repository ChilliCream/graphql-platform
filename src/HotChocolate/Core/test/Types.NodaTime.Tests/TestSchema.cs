using NodaTime;

namespace HotChocolate.Types.NodaTime.Tests;

public static class TestSchema
{
    public class Query
    {
        public ZonedDateTime Rome
            => new ZonedDateTime(
                LocalDateTime.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                DateTimeZoneProviders.Tzdb["Asia/Kathmandu"],
                Offset.FromHoursAndMinutes(5, 45));

        public ZonedDateTime Utc
            => new ZonedDateTime(
                LocalDateTime.FromDateTime(new DateTime(2020, 12, 31, 18, 30, 13)),
                DateTimeZoneProviders.Tzdb["UTC"],
                Offset.FromHours(0));
    }

    public class Mutation
    {
        public ZonedDateTime Test(ZonedDateTime arg)
            => arg + Duration.FromMinutes(10);
    }
}
