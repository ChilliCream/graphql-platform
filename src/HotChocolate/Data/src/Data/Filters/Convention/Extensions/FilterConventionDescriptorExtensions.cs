using System;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data
{
    public static class FilterConventionDescriptorExtensions
    {
        public static IFilterConventionDescriptor AddDefaults(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.AddDefaultOperations().BindDefaultTypes().UseQueryableProvider();

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
                .BindRuntimeType<sbyte, ComparableOperationFilterInputType<sbyte>>()
                .BindRuntimeType<ushort, ComparableOperationFilterInputType<ushort>>()
                .BindRuntimeType<uint, ComparableOperationFilterInputType<uint>>()
                .BindRuntimeType<ulong, ComparableOperationFilterInputType<ulong>>()
                
            return descriptor;
        }
    }
}
