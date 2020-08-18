using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Filters
{
    public static class FilterConventionDescriptorExtensions
    {
        public static IFilterConventionDescriptor UseDefault(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.UseDefaultOperations().UseDefaultFields().UseQueryableProvider();

        public static IFilterConventionDescriptor UseDefaultOperations(
            this IFilterConventionDescriptor descriptor)
        {
            if (descriptor == null)
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

        public static IFilterConventionDescriptor UseDefaultFields(
            this IFilterConventionDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }
            
            descriptor.Binding<string, StringOperationInput>();
            descriptor.Binding<bool, BooleanOperationInput>();
            descriptor.Binding<byte, ComparableOperationInput<byte>>();
            descriptor.Binding<short, ComparableOperationInput<short>>();
            descriptor.Binding<int, ComparableOperationInput<int>>();
            descriptor.Binding<long, ComparableOperationInput<long>>();
            descriptor.Binding<float, ComparableOperationInput<float>>();
            descriptor.Binding<double, ComparableOperationInput<double>>();
            descriptor.Binding<decimal, ComparableOperationInput<decimal>>();
            descriptor.Binding<bool?, BooleanOperationInput>();
            descriptor.Binding<byte?, ComparableOperationInput<byte?>>();
            descriptor.Binding<short?, ComparableOperationInput<short?>>();
            descriptor.Binding<int?, ComparableOperationInput<int?>>();
            descriptor.Binding<long?, ComparableOperationInput<long?>>();
            descriptor.Binding<float?, ComparableOperationInput<float?>>();
            descriptor.Binding<double?, ComparableOperationInput<double?>>();
            descriptor.Binding<decimal?, ComparableOperationInput<decimal?>>();
            descriptor.Binding<Guid, ComparableOperationInput<Guid>>();
            descriptor.Binding<DateTime, ComparableOperationInput<DateTime>>();
            descriptor.Binding<DateTimeOffset, ComparableOperationInput<DateTimeOffset>>();
            descriptor.Binding<TimeSpan, ComparableOperationInput<TimeSpan>>();
            return descriptor;
        }
    }
}
