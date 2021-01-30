using System;
using Neo4j.Driver;
using ServiceStack;

namespace HotChocolate.Data.Neo4J
{
    public static class MapperConfig
    {
        public static void RegisterTypeConverters()
        {
            RegisterLocalDateToDateTimeConverter();
            RegisterLocalTimeToTimeSpanConverter();
            RegisterZonedDateTimeToDateTimeOffsetConverter();
            RegisterLocalDateTimeToDateTimeConverter();
        }

        public static void RegisterLocalDateToDateTimeConverter()
        {
            AutoMapping.RegisterConverter<LocalDate, DateTime>(localDate => localDate?.ToDateTime() ?? default);
            AutoMapping.RegisterConverter<LocalDate, DateTime?>(localDateTime => localDateTime?.ToDateTime());
            AutoMapping.RegisterConverter<DateTime, LocalDate>(dateTime => new LocalDate(dateTime));
            AutoMapping.RegisterConverter((DateTime? from) => from.HasValue ? new LocalDate(from.Value) : null);
        }

        public static void RegisterLocalTimeToTimeSpanConverter()
        {
            AutoMapping.RegisterConverter<LocalTime, TimeSpan>(localTime => localTime?.ToTimeSpan() ?? default);
            AutoMapping.RegisterConverter<LocalTime, TimeSpan?>(localTime => localTime?.ToTimeSpan());
            AutoMapping.RegisterConverter<TimeSpan, LocalTime>(timeSpan => new LocalTime(timeSpan));
            AutoMapping.RegisterConverter((TimeSpan? from) => from.HasValue ? new LocalTime(from.Value) : null);
        }

        public static void RegisterZonedDateTimeToDateTimeOffsetConverter()
        {
            AutoMapping.RegisterConverter<ZonedDateTime, DateTimeOffset>(zonedDateTime => zonedDateTime?.ToDateTimeOffset() ?? default);
            AutoMapping.RegisterConverter<ZonedDateTime, DateTimeOffset?>(zonedDateTime => zonedDateTime?.ToDateTimeOffset());
            AutoMapping.RegisterConverter<DateTimeOffset, ZonedDateTime>(dateTimeOffset => new ZonedDateTime(dateTimeOffset));
            AutoMapping.RegisterConverter((DateTimeOffset? from) => from.HasValue ? new ZonedDateTime(from.Value) : null);
        }

        public static void RegisterLocalDateTimeToDateTimeConverter()
        {
            AutoMapping.RegisterConverter<LocalDateTime, DateTime>(localDateTime => localDateTime?.ToDateTime() ?? default);
            AutoMapping.RegisterConverter<LocalDateTime, DateTime?>(localDateTime => localDateTime?.ToDateTime());
            AutoMapping.RegisterConverter<DateTime, LocalDateTime>(dateTime => new LocalDateTime(dateTime));
            AutoMapping.RegisterConverter((DateTime? from) => from.HasValue ? new LocalDateTime(from.Value) : null);
        }
    }
}
