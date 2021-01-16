using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data
{
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
            descriptor.AddDefaultOperations().BindDefaultTypes().UseQueryableProvider();

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
        /// <returns>The descriptor that was passed in as a parameter</returns>
        /// <exception cref="ArgumentNullException">
        /// Throws in case the argument <paramref name="descriptor"/> is null
        /// </exception>
        public static IFilterConventionDescriptor BindDefaultTypes(
            this IFilterConventionDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor
                .BindRuntimeType<string, StringOperationFilterInputType>()
                .BindRuntimeType<bool, BooleanOperationFilterInputType>()
                .BindRuntimeType<byte, ComparableOperationFilterInputType<byte>>()
                .BindRuntimeType<short, ComparableOperationFilterInputType<short>>()
                .BindRuntimeType<int, ComparableOperationFilterInputType<int>>()
                .BindRuntimeType<long, ComparableOperationFilterInputType<long>>()
                .BindRuntimeType<float, ComparableOperationFilterInputType<float>>()
                .BindRuntimeType<double, ComparableOperationFilterInputType<double>>()
                .BindRuntimeType<decimal, ComparableOperationFilterInputType<decimal>>()
                .BindRuntimeType<Guid, ComparableOperationFilterInputType<Guid>>()
                .BindRuntimeType<DateTime, ComparableOperationFilterInputType<DateTime>>()
                .BindRuntimeType<DateTimeOffset,
                    ComparableOperationFilterInputType<DateTimeOffset>>()
                .BindRuntimeType<TimeSpan, ComparableOperationFilterInputType<TimeSpan>>()
                .BindRuntimeType<bool?, BooleanOperationFilterInputType>()
                .BindRuntimeType<byte?, ComparableOperationFilterInputType<byte?>>()
                .BindRuntimeType<short?, ComparableOperationFilterInputType<short?>>()
                .BindRuntimeType<int?, ComparableOperationFilterInputType<int?>>()
                .BindRuntimeType<long?, ComparableOperationFilterInputType<long?>>()
                .BindRuntimeType<float?, ComparableOperationFilterInputType<float?>>()
                .BindRuntimeType<double?, ComparableOperationFilterInputType<double?>>()
                .BindRuntimeType<decimal?, ComparableOperationFilterInputType<decimal?>>()
                .BindRuntimeType<Guid?, ComparableOperationFilterInputType<Guid?>>()
                .BindRuntimeType<DateTime?, ComparableOperationFilterInputType<DateTime?>>()
                .BindRuntimeType<DateTimeOffset?,
                    ComparableOperationFilterInputType<DateTimeOffset?>>()
                .BindRuntimeType<TimeSpan?, ComparableOperationFilterInputType<TimeSpan?>>();

            return descriptor;
        }
    }
}
