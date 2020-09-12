using System;

namespace HotChocolate.Data.Filters
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

            descriptor.Operation(DefaultOperations.Equals).Name("eq");
            descriptor.Operation(DefaultOperations.NotEquals).Name("neq");
            descriptor.Operation(DefaultOperations.GreaterThan).Name("gt");
            descriptor.Operation(DefaultOperations.NotGreaterThan).Name("ngt");
            descriptor.Operation(DefaultOperations.GreaterThanOrEquals).Name("gte");
            descriptor.Operation(DefaultOperations.NotGreaterThanOrEquals).Name("ngte");
            descriptor.Operation(DefaultOperations.LowerThan).Name("lt");
            descriptor.Operation(DefaultOperations.NotLowerThan).Name("nlt");
            descriptor.Operation(DefaultOperations.LowerThanOrEquals).Name("lte");
            descriptor.Operation(DefaultOperations.NotLowerThanOrEquals).Name("nlte");
            descriptor.Operation(DefaultOperations.Contains).Name("contains");
            descriptor.Operation(DefaultOperations.NotContains).Name("ncontains");
            descriptor.Operation(DefaultOperations.In).Name("in");
            descriptor.Operation(DefaultOperations.NotIn).Name("nin");
            descriptor.Operation(DefaultOperations.StartsWith).Name("startsWith");
            descriptor.Operation(DefaultOperations.NotStartsWith).Name("nstartsWith");
            descriptor.Operation(DefaultOperations.EndsWith).Name("endsWith");
            descriptor.Operation(DefaultOperations.NotEndsWith).Name("nendsWith");
            descriptor.Operation(DefaultOperations.All).Name("all");
            descriptor.Operation(DefaultOperations.None).Name("none");
            descriptor.Operation(DefaultOperations.Some).Name("some");
            descriptor.Operation(DefaultOperations.Any).Name("any");
            descriptor.Operation(DefaultOperations.And).Name("and");
            descriptor.Operation(DefaultOperations.Or).Name("or");
            descriptor.Operation(DefaultOperations.Data).Name("data");

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
                .BindRuntimeType<string, StringOperationFilterInput>()
                .BindRuntimeType<bool, BooleanOperationFilterInput>()
                .BindRuntimeType<byte, ComparableOperationFilterInput<byte>>()
                .BindRuntimeType<short, ComparableOperationFilterInput<short>>()
                .BindRuntimeType<int, ComparableOperationFilterInput<int>>()
                .BindRuntimeType<long, ComparableOperationFilterInput<long>>()
                .BindRuntimeType<float, ComparableOperationFilterInput<float>>()
                .BindRuntimeType<double, ComparableOperationFilterInput<double>>()
                .BindRuntimeType<decimal, ComparableOperationFilterInput<decimal>>()
                .BindRuntimeType<Guid, ComparableOperationFilterInput<Guid>>()
                .BindRuntimeType<DateTime, ComparableOperationFilterInput<DateTime>>()
                .BindRuntimeType<DateTimeOffset, ComparableOperationFilterInput<DateTimeOffset>>()
                .BindRuntimeType<TimeSpan, ComparableOperationFilterInput<TimeSpan>>()
                .BindRuntimeType<bool?, BooleanOperationFilterInput>()
                .BindRuntimeType<byte?, ComparableOperationFilterInput<byte?>>()
                .BindRuntimeType<short?, ComparableOperationFilterInput<short?>>()
                .BindRuntimeType<int?, ComparableOperationFilterInput<int?>>()
                .BindRuntimeType<long?, ComparableOperationFilterInput<long?>>()
                .BindRuntimeType<float?, ComparableOperationFilterInput<float?>>()
                .BindRuntimeType<double?, ComparableOperationFilterInput<double?>>()
                .BindRuntimeType<decimal?, ComparableOperationFilterInput<decimal?>>()
                .BindRuntimeType<Guid?, ComparableOperationFilterInput<Guid?>>()
                .BindRuntimeType<DateTime?, ComparableOperationFilterInput<DateTime?>>()
                .BindRuntimeType<DateTimeOffset?, ComparableOperationFilterInput<DateTimeOffset?>>()
                .BindRuntimeType<TimeSpan?, ComparableOperationFilterInput<TimeSpan?>>();

            return descriptor;
        }
    }
}
