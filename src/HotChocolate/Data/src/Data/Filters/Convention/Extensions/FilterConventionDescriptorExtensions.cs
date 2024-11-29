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
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

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
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (compatibilityMode)
        {
            return descriptor
                .BindRuntimeType<string, StringOperationFilterInputType>()
                .BindRuntimeType<bool, BooleanOperationFilterInputType>()
                .BindRuntimeType<bool?, BooleanOperationFilterInputType>()
                .BindComparableType<byte>()
                .BindComparableType<short>()
                .BindComparableType<int>()
                .BindComparableType<long>()
                .BindComparableType<float>()
                .BindComparableType<double>()
                .BindComparableType<decimal>()
                .BindComparableType<sbyte>()
                .BindComparableType<ushort>()
                .BindComparableType<uint>()
                .BindComparableType<ulong>()
                .BindComparableType<Guid>()
                .BindComparableType<DateTime>()
                .BindComparableType<DateTimeOffset>()
                .BindComparableType<DateOnly>()
                .BindComparableType<TimeOnly>()
                .BindComparableType<TimeSpan>()
                .BindRuntimeType<Uri, ComparableOperationFilterInputType<Uri>>()
                .BindRuntimeType<Uri?, ComparableOperationFilterInputType<Uri?>>();
        }
        else
        {
            return descriptor
                .BindRuntimeType<string, StringOperationFilterInputType>()
                .BindRuntimeType<bool, BooleanOperationFilterInputType>()
                .BindRuntimeType<bool?, BooleanOperationFilterInputType>()
                .BindRuntimeType<byte, ByteOperationFilterInputType>()
                .BindRuntimeType<byte?, ByteOperationFilterInputType>()
                .BindRuntimeType<sbyte, ByteOperationFilterInputType>()
                .BindRuntimeType<sbyte?, ByteOperationFilterInputType>()
                .BindRuntimeType<short, ShortOperationFilterInputType>()
                .BindRuntimeType<short?, ShortOperationFilterInputType>()
                .BindRuntimeType<int, IntOperationFilterInputType>()
                .BindRuntimeType<int?, IntOperationFilterInputType>()
                .BindRuntimeType<long, LongOperationFilterInputType>()
                .BindRuntimeType<long?, LongOperationFilterInputType>()
                .BindRuntimeType<float, FloatOperationFilterInputType>()
                .BindRuntimeType<float?, FloatOperationFilterInputType>()
                .BindRuntimeType<double, FloatOperationFilterInputType>()
                .BindRuntimeType<double?, FloatOperationFilterInputType>()
                .BindRuntimeType<decimal, DecimalOperationFilterInputType>()
                .BindRuntimeType<decimal?, DecimalOperationFilterInputType>()
                .BindRuntimeType<Guid, UuidOperationFilterInputType>()
                .BindRuntimeType<Guid?, UuidOperationFilterInputType>()
                .BindRuntimeType<DateTime, DateTimeOperationFilterInputType>()
                .BindRuntimeType<DateTime?, DateTimeOperationFilterInputType>()
                .BindRuntimeType<DateTimeOffset, DateTimeOperationFilterInputType>()
                .BindRuntimeType<DateTimeOffset?, DateTimeOperationFilterInputType>()
                .BindRuntimeType<DateOnly, DateOperationFilterInputType>()
                .BindRuntimeType<DateOnly?, DateOperationFilterInputType>()
                .BindRuntimeType<TimeOnly, TimeSpanOperationFilterInputType>()
                .BindRuntimeType<TimeOnly?, TimeSpanOperationFilterInputType>()
                .BindRuntimeType<TimeSpan, TimeSpanOperationFilterInputType>()
                .BindRuntimeType<TimeSpan?, TimeSpanOperationFilterInputType>()
                .BindRuntimeType<Uri, UrlOperationFilterInputType>()
                .BindRuntimeType<Uri?, UrlOperationFilterInputType>();
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
