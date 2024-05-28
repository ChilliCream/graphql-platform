using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Types.NodaTime.Tests
{
    public static class NodaTimeRequestExecutorBuilderExtensions
    {
        public static ISchemaBuilder AddNodaTime(
            this ISchemaBuilder schemaBuilder,
            params Type[] excludeTypes)
        {
            foreach (var type in _nodaTimeTypes.Except(excludeTypes))
            {
                schemaBuilder = schemaBuilder.AddType(type);
            }

            return schemaBuilder;
        }

        private static readonly IReadOnlyList<Type> _nodaTimeTypes = new[]
        {
            typeof(DateTimeZoneType), typeof(DurationType), typeof(InstantType),
            typeof(IsoDayOfWeekType), typeof(LocalDateTimeType), typeof(LocalDateType),
            typeof(LocalTimeType), typeof(OffsetDateTimeType), typeof(OffsetDateType),
            typeof(OffsetTimeType), typeof(OffsetType), typeof(PeriodType),
            typeof(ZonedDateTimeType),
        };
    }
}
