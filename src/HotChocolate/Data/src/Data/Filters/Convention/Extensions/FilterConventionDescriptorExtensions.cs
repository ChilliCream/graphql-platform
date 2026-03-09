using HotChocolate.Data.Filters;

namespace HotChocolate.Data;

public static class FilterConventionDescriptorExtensions
{
    /// <summary>
    /// Initializes the default configuration for <see cref="IQueryable"/> and
    /// <see cref="IEnumerable{T}"/> on the convention
    /// </summary>
    /// <param name="descriptor">The descriptor where the handlers are registered</param>
    /// <returns>The descriptor that was passed in as a parameter</returns>
    public static IFilterConventionDescriptor AddDefaults(
        this IFilterConventionDescriptor descriptor) =>
        descriptor.AddDefaults(false);

    /// <summary>
    /// Initializes the default configuration for <see cref="IQueryable"/> and
    /// <see cref="IEnumerable{T}"/> on the convention
    /// </summary>
    /// <param name="descriptor">The descriptor where the handlers are registered</param>
    /// <param name="compatibilityMode">Uses the old behavior of naming the filters</param>
    /// <returns>The descriptor that was passed in as a parameter</returns>
    public static IFilterConventionDescriptor AddDefaults(
        this IFilterConventionDescriptor descriptor,
        bool compatibilityMode) =>
        descriptor
            .AddDefaultOperations()
            .BindDefaultTypes(compatibilityMode)
            .UseQueryableProvider();

    /// <summary>
    /// Adds default operations to the descriptor
    /// </summary>
    /// <param name="descriptor">The descriptor where the handlers are registered</param>
    /// <returns>The descriptor that was passed in as a parameter</returns>
    /// <exception cref="ArgumentNullException">
    /// Throws in case the argument <paramref name="descriptor"/> is null
    /// </exception>
    public static IFilterConventionDescriptor AddDefaultOperations(
        this IFilterConventionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor.Operation(DefaultFilterOperations.Equals).Name("eq");
        descriptor.Operation(DefaultFilterOperations.NotEquals).Name("neq");
        descriptor.Operation(DefaultFilterOperations.GreaterThan).Name("gt");
        descriptor.Operation(DefaultFilterOperations.NotGreaterThan).Name("ngt");
        descriptor.Operation(DefaultFilterOperations.GreaterThanOrEquals).Name("gte");
        descriptor.Operation(DefaultFilterOperations.NotGreaterThanOrEquals).Name("ngte");
        descriptor.Operation(DefaultFilterOperations.LowerThan).Name("lt");
        descriptor.Operation(DefaultFilterOperations.NotLowerThan).Name("nlt");
        descriptor.Operation(DefaultFilterOperations.LowerThanOrEquals).Name("lte");
        descriptor.Operation(DefaultFilterOperations.NotLowerThanOrEquals).Name("nlte");
        descriptor.Operation(DefaultFilterOperations.Contains).Name("contains");
        descriptor.Operation(DefaultFilterOperations.NotContains).Name("ncontains");
        descriptor.Operation(DefaultFilterOperations.In).Name("in");
        descriptor.Operation(DefaultFilterOperations.NotIn).Name("nin");
        descriptor.Operation(DefaultFilterOperations.StartsWith).Name("startsWith");
        descriptor.Operation(DefaultFilterOperations.NotStartsWith).Name("nstartsWith");
        descriptor.Operation(DefaultFilterOperations.EndsWith).Name("endsWith");
        descriptor.Operation(DefaultFilterOperations.NotEndsWith).Name("nendsWith");
        descriptor.Operation(DefaultFilterOperations.All).Name("all");
        descriptor.Operation(DefaultFilterOperations.None).Name("none");
        descriptor.Operation(DefaultFilterOperations.Some).Name("some");
        descriptor.Operation(DefaultFilterOperations.Any).Name("any");
        descriptor.Operation(DefaultFilterOperations.And).Name("and");
        descriptor.Operation(DefaultFilterOperations.Or).Name("or");
        descriptor.Operation(DefaultFilterOperations.Data).Name("data");

        return descriptor;
    }

    /// <summary>
    /// Binds common runtime types to the according <see cref="FilterInputType"/>
    /// </summary>
    /// <param name="descriptor">The descriptor where the handlers are registered</param>
    /// <param name="compatibilityMode">Uses the old behavior of naming the filters</param>
    /// <returns>The descriptor that was passed in as a parameter</returns>
    /// <exception cref="ArgumentNullException">
    /// Throws in case the argument <paramref name="descriptor"/> is null
    /// </exception>
    public static IFilterConventionDescriptor BindDefaultTypes(
        this IFilterConventionDescriptor descriptor,
        bool compatibilityMode = false)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        if (compatibilityMode)
        {
            return descriptor
                .BindComparableType<byte>()
                .BindComparableType<DateOnly>()
                .BindComparableType<DateTime>()
                .BindComparableType<DateTimeOffset>()
                .BindComparableType<decimal>()
                .BindComparableType<double>()
                .BindComparableType<float>()
                .BindComparableType<Guid>()
                .BindComparableType<int>()
                .BindComparableType<long>()
                .BindComparableType<sbyte>()
                .BindComparableType<short>()
                .BindComparableType<TimeOnly>()
                .BindComparableType<TimeSpan>()
                .BindComparableType<uint>()
                .BindComparableType<ulong>()
                .BindComparableType<ushort>()
                .BindRuntimeType<bool, BooleanOperationFilterInputType>()
                .BindRuntimeType<bool?, BooleanOperationFilterInputType>()
                .BindRuntimeType<string, StringOperationFilterInputType>()
                .BindRuntimeType<Uri, ComparableOperationFilterInputType<Uri>>()
                .BindRuntimeType<Uri?, ComparableOperationFilterInputType<Uri?>>();
        }
        else
        {
            return descriptor
                .BindRuntimeType<bool, BooleanOperationFilterInputType>()
                .BindRuntimeType<bool?, BooleanOperationFilterInputType>()
                .BindRuntimeType<byte, UnsignedByteOperationFilterInputType>()
                .BindRuntimeType<byte?, UnsignedByteOperationFilterInputType>()
                .BindRuntimeType<DateOnly, LocalDateOperationFilterInputType>()
                .BindRuntimeType<DateOnly?, LocalDateOperationFilterInputType>()
                .BindRuntimeType<DateTime, DateTimeOperationFilterInputType>()
                .BindRuntimeType<DateTime?, DateTimeOperationFilterInputType>()
                .BindRuntimeType<DateTimeOffset, DateTimeOperationFilterInputType>()
                .BindRuntimeType<DateTimeOffset?, DateTimeOperationFilterInputType>()
                .BindRuntimeType<decimal, DecimalOperationFilterInputType>()
                .BindRuntimeType<decimal?, DecimalOperationFilterInputType>()
                .BindRuntimeType<double, FloatOperationFilterInputType>()
                .BindRuntimeType<double?, FloatOperationFilterInputType>()
                .BindRuntimeType<float, FloatOperationFilterInputType>()
                .BindRuntimeType<float?, FloatOperationFilterInputType>()
                .BindRuntimeType<Guid, UuidOperationFilterInputType>()
                .BindRuntimeType<Guid?, UuidOperationFilterInputType>()
                .BindRuntimeType<int, IntOperationFilterInputType>()
                .BindRuntimeType<int?, IntOperationFilterInputType>()
                .BindRuntimeType<long, LongOperationFilterInputType>()
                .BindRuntimeType<long?, LongOperationFilterInputType>()
                .BindRuntimeType<sbyte, ByteOperationFilterInputType>()
                .BindRuntimeType<sbyte?, ByteOperationFilterInputType>()
                .BindRuntimeType<short, ShortOperationFilterInputType>()
                .BindRuntimeType<short?, ShortOperationFilterInputType>()
                .BindRuntimeType<string, StringOperationFilterInputType>()
                .BindRuntimeType<TimeOnly, LocalTimeOperationFilterInputType>()
                .BindRuntimeType<TimeOnly?, LocalTimeOperationFilterInputType>()
                .BindRuntimeType<TimeSpan, TimeSpanOperationFilterInputType>()
                .BindRuntimeType<TimeSpan?, TimeSpanOperationFilterInputType>()
                .BindRuntimeType<uint, UnsignedIntOperationFilterInputType>()
                .BindRuntimeType<uint?, UnsignedIntOperationFilterInputType>()
                .BindRuntimeType<ulong, UnsignedLongOperationFilterInputType>()
                .BindRuntimeType<ulong?, UnsignedLongOperationFilterInputType>()
                .BindRuntimeType<Uri, UriOperationFilterInputType>()
                .BindRuntimeType<Uri?, UriOperationFilterInputType>()
                .BindRuntimeType<ushort, UnsignedShortOperationFilterInputType>()
                .BindRuntimeType<ushort?, UnsignedShortOperationFilterInputType>();
        }
    }

    private static IFilterConventionDescriptor BindComparableType<T>(
        this IFilterConventionDescriptor descriptor)
        where T : struct
    {
        return descriptor
            .BindRuntimeType<T, ComparableOperationFilterInputType<T>>()
            .BindRuntimeType<T?, ComparableOperationFilterInputType<T?>>();
    }
}
