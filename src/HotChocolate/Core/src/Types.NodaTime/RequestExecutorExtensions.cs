using System.Collections.Immutable;
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
        /// <param name="excludedScalarTypes">Scalar types that should be excluded.</param>
        /// <param name="bindBclTypes">Whether to bind equivalent BCL types to the scalar types.</param>
        /// <returns>The request executor builder.</returns>
        /// <remarks>
        /// <b>Warning:</b> Binding BCL types to NodaTime scalars can result in loss of precision.
        /// For example, NodaTime types support nanosecond precision, but BCL types only support up
        /// to 100-nanosecond precision. Use with caution if your application relies on
        /// high-precision date and time values.
        /// </remarks>
        public IRequestExecutorBuilder AddNodaTime(
            ImmutableArray<Type>? excludedScalarTypes = null,
            bool bindBclTypes = false)
        {
            Type[] types =
            [
                typeof(DateTimeType),
                typeof(DurationType),
                typeof(LocalDateTimeType),
                typeof(LocalDateType),
                typeof(LocalTimeType)
            ];

            var includedTypes =
                excludedScalarTypes is null
                    ? types
                    : types.Except(excludedScalarTypes).ToArray();

            foreach (var type in includedTypes)
            {
                builder.AddType(type);

                if (bindBclTypes)
                {
                    s_configByScalarType[type](builder);
                }
            }

            return builder;
        }
    }

    private static readonly Dictionary<Type, Action<IRequestExecutorBuilder>> s_configByScalarType = new()
    {
        {
            typeof(DateTimeType),
            b => b
                .BindRuntimeType<DateTimeOffset, DateTimeType>()
                .AddTypeConverter<DateTimeOffset, OffsetDateTime>(d => d.ToOffsetDateTime())
                .AddTypeConverter<OffsetDateTime, DateTimeOffset>(o => o.ToDateTimeOffset())
        },
        {
            typeof(DurationType),
            b => b
                .BindRuntimeType<TimeSpan, DurationType>()
                .AddTypeConverter<TimeSpan, Duration>(Duration.FromTimeSpan)
                .AddTypeConverter<Duration, TimeSpan>(d => d.ToTimeSpan())
        },
        {
            typeof(LocalDateTimeType),
            b => b
                .BindRuntimeType<DateTime, LocalDateTimeType>()
                .AddTypeConverter<DateTime, LocalDateTime>(d => d.ToLocalDateTime())
                .AddTypeConverter<LocalDateTime, DateTime>(l => l.ToDateTimeUnspecified())
        },
        {
            typeof(LocalDateType),
            b => b
                .BindRuntimeType<DateOnly, LocalDateType>()
                .AddTypeConverter<DateOnly, LocalDate>(d => d.ToLocalDate())
                .AddTypeConverter<LocalDate, DateOnly>(l => l.ToDateOnly())
        },
        {
            typeof(LocalTimeType),
            b => b
                .BindRuntimeType<TimeOnly, LocalTimeType>()
                .AddTypeConverter<TimeOnly, LocalTime>(t => t.ToLocalTime())
                .AddTypeConverter<LocalTime, TimeOnly>(l => l.ToTimeOnly())
        }
    };
}
