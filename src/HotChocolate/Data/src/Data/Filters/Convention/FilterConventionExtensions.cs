using System;

namespace HotChocolate.Data.Filters
{
    public static class FilterConventionExtensions
    {
        public static IFilterConventionDescriptor UseDefault(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.UseDefaultOperations().UseDefaultFields();

        public static IFilterConventionDescriptor UseDefaultOperations(
            this IFilterConventionDescriptor descriptor)
        {
            descriptor.Operation(Operations.Equals).Name("eq");
            descriptor.Operation(Operations.NotEquals).Name("neq");
            descriptor.Operation(Operations.GreaterThan).Name("gt");
            descriptor.Operation(Operations.NotGreaterThan).Name("ngt");
            descriptor.Operation(Operations.GreaterThanOrEquals).Name("gte");
            descriptor.Operation(Operations.NotGreaterThanOrEquals).Name("ngte");
            descriptor.Operation(Operations.LowerThan).Name("lt");
            descriptor.Operation(Operations.NotLowerThan).Name("nlt");
            descriptor.Operation(Operations.LowerThanOrEquals).Name("lte");
            descriptor.Operation(Operations.NotLowerThanOrEquals).Name("nlte");
            descriptor.Operation(Operations.Contains).Name("contains");
            descriptor.Operation(Operations.NotContains).Name("ncontains");
            descriptor.Operation(Operations.In).Name("in");
            descriptor.Operation(Operations.NotIn).Name("nin");
            descriptor.Operation(Operations.StartsWith).Name("startsWith");
            descriptor.Operation(Operations.NotStartsWith).Name("nstartsWith");
            descriptor.Operation(Operations.EndsWith).Name("endsWith");
            descriptor.Operation(Operations.NotEndsWith).Name("nendsWith");
            descriptor.Operation(Operations.All).Name("all");
            descriptor.Operation(Operations.None).Name("none");
            descriptor.Operation(Operations.Some).Name("some");
            descriptor.Operation(Operations.Any).Name("any");
            descriptor.Operation(Operations.And).Name("and");
            descriptor.Operation(Operations.Or).Name("or");
            return descriptor;
        }

        public static IFilterConventionDescriptor UseDefaultFields(
            this IFilterConventionDescriptor descriptor)
        {
            descriptor.Binding<string, StringOperationInput>();
            descriptor.Binding<bool, BooleanOperationInput>();
            descriptor.Binding<byte, ComparableOperationInput<byte>>();
            descriptor.Binding<short, ComparableOperationInput<short>>();
            descriptor.Binding<int, ComparableOperationInput<int>>();
            descriptor.Binding<long, ComparableOperationInput<long>>();
            descriptor.Binding<float, ComparableOperationInput<float>>();
            descriptor.Binding<double, ComparableOperationInput<double>>();
            descriptor.Binding<decimal, ComparableOperationInput<decimal>>();
            descriptor.Binding<Guid, ComparableOperationInput<Guid>>();
            descriptor.Binding<DateTime, ComparableOperationInput<DateTime>>();
            descriptor.Binding<DateTimeOffset, ComparableOperationInput<DateTimeOffset>>();
            descriptor.Binding<TimeSpan, ComparableOperationInput<TimeSpan>>();
            return descriptor;
        }
    }
}
