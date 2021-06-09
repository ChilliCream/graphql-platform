namespace HotChocolate.Types.NodaTime.Tests
{
    public static class NodaTimeRequestExecutorBuilderExtensions
    {
        public static ISchemaBuilder AddNodaTime(this ISchemaBuilder schemaBuilder)
        {
            return schemaBuilder
                .AddType<DateTimeZoneType>()
                .AddType<DurationType>()
                .AddType<InstantType>()
                .AddType<IsoDayOfWeekType>()
                .AddType<LocalDateTimeType>()
                .AddType<LocalDateType>()
                .AddType<LocalTimeType>()
                .AddType<OffsetDateTimeType>()
                .AddType<OffsetDateType>()
                .AddType<OffsetTimeType>()
                .AddType<OffsetType>()
                .AddType<PeriodType>()
                .AddType<ZonedDateTimeType>();
        }
    }
}
