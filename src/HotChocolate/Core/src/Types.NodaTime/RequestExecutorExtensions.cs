using HotChocolate.Execution.Configuration;
using NodaTime;
using NodaTime.Extensions;

namespace HotChocolate.Types.NodaTime;

/// <summary>
/// Extension methods for <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class RequestExecutorExtensions
{
    extension(IRequestExecutorBuilder builder)
    {
        /// <summary>
        /// Adds NodaTime scalar types to the schema.
        /// </summary>
        /// <returns>The request executor builder.</returns>
        public IRequestExecutorBuilder AddNodaTime()
        {
            return builder
                .AddType<DateTimeType>()
                .AddType<DurationType>()
                .AddType<LocalDateTimeType>()
                .AddType<LocalDateType>()
                .AddType<LocalTimeType>()
                .BindRuntimeType<DateTimeOffset, DateTimeType>()
                .BindRuntimeType<DateTime, LocalDateTimeType>()
                .BindRuntimeType<DateOnly, LocalDateType>()
                .BindRuntimeType<TimeOnly, LocalTimeType>()
                .AddTypeConverter<DateTimeOffset, OffsetDateTime>(d => d.ToOffsetDateTime())
                .AddTypeConverter<OffsetDateTime, DateTimeOffset>(o => o.ToDateTimeOffset())
                .AddTypeConverter<DateTime, LocalDateTime>(d => d.ToLocalDateTime())
                .AddTypeConverter<LocalDateTime, DateTime>(l => l.ToDateTimeUnspecified())
                .AddTypeConverter<DateOnly, LocalDate>(d => d.ToLocalDate())
                .AddTypeConverter<LocalDate, DateOnly>(l => l.ToDateOnly())
                .AddTypeConverter<TimeOnly, LocalTime>(t => t.ToLocalTime())
                .AddTypeConverter<LocalTime, TimeOnly>(l => l.ToTimeOnly());
        }
    }
}
