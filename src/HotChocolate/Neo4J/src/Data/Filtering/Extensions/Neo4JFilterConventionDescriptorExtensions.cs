using System;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data.Neo4J.Filtering
{
    public static class Neo4JFilterConventionDescriptorExtensions
    {
        /// <summary>
        /// Initializes the default configuration for Neo4j on the convention by adding operations
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        public static IFilterConventionDescriptor AddNeo4JDefaults(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.AddDefaultNeo4JOperations().BindDefaultNeo4JTypes().UseNeo4JProvider();

        /// <summary>
        /// Adds default operations for Neo4j to the descriptor
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/></returns>
        /// <exception cref="ArgumentNullException">
        /// Throws in case the argument <paramref name="descriptor"/> is null
        /// </exception>
        public static IFilterConventionDescriptor AddDefaultNeo4JOperations(
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
            //descriptor.Operation(DefaultFilterOperations.Data).Name("data");

            return descriptor;
        }

        /// <summary>
        /// Binds common runtime types to the according <see cref="FilterInputType"/> that are
        /// supported by Neo4j
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The descriptor that was passed in as a parameter</returns>
        /// <exception cref="ArgumentNullException">
        /// Throws in case the argument <paramref name="descriptor"/> is null
        /// </exception>
        public static IFilterConventionDescriptor BindDefaultNeo4JTypes(
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
                .BindRuntimeType<DateTimeOffset, ComparableOperationFilterInputType<DateTimeOffset>
                >()
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

        /// <summary>
        /// Adds a <see cref="Neo4JFilterProvider"/> with default configuration
        /// </summary>
        /// <param name="descriptor">The descriptor where the provider is registered</param>
        /// <returns>The descriptor that was passed in as a parameter</returns>
        public static IFilterConventionDescriptor UseNeo4JProvider(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.Provider(new Neo4JFilterProvider(x => x.AddDefaultNeo4JFieldHandlers()));

        /// <summary>
        /// Initializes the default configuration of the provider by registering handlers
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/> that was passed in as a parameter</returns>
        public static IFilterProviderDescriptor<Neo4JFilterVisitorContext>
            AddDefaultNeo4JFieldHandlers(
                this IFilterProviderDescriptor<Neo4JFilterVisitorContext> descriptor)
        {
            descriptor.AddFieldHandler<Neo4JEqualsOperationHandler>();
            descriptor.AddFieldHandler<Neo4JNotEqualsOperationHandler>();

            descriptor.AddFieldHandler<Neo4JInOperationHandler>();
            descriptor.AddFieldHandler<Neo4JNotInOperationHandler>();

            descriptor.AddFieldHandler<Neo4JComparableGreaterThanHandler>();
            descriptor.AddFieldHandler<Neo4JComparableNotGreaterThanHandler>();
            descriptor.AddFieldHandler<Neo4JComparableGreaterThanOrEqualsHandler>();
            descriptor.AddFieldHandler<Neo4JComparableNotGreaterThanOrEqualsHandler>();
            descriptor.AddFieldHandler<Neo4JComparableLowerThanHandler>();
            descriptor.AddFieldHandler<Neo4JComparableNotLowerThanHandler>();
            descriptor.AddFieldHandler<Neo4JComparableLowerThanOrEqualsHandler>();
            descriptor.AddFieldHandler<Neo4JComparableNotLowerThanOrEqualsHandler>();

            descriptor.AddFieldHandler<Neo4JStringStartsWithHandler>();
            descriptor.AddFieldHandler<Neo4JStringNotStartsWithHandler>();
            descriptor.AddFieldHandler<Neo4JStringEndsWithHandler>();
            descriptor.AddFieldHandler<Neo4JStringNotEndsWithHandler>();
            descriptor.AddFieldHandler<Neo4JStringContainsHandler>();
            descriptor.AddFieldHandler<Neo4JStringNotContainsHandler>();

            descriptor.AddFieldHandler<Neo4JListAllOperationHandler>();
            descriptor.AddFieldHandler<Neo4JListAnyOperationHandler>();
            descriptor.AddFieldHandler<Neo4JListNoneOperationHandler>();
            descriptor.AddFieldHandler<Neo4JListSomeOperationHandler>();

            descriptor.AddFieldHandler<Neo4JDefaultFieldHandler>();

            return descriptor;
        }
    }
}
