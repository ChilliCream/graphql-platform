using HotChocolate.Data.Filters;

namespace HotChocolate.Data.ElasticSearch.Filters
{
    public static class FilterConventionDescriptorElasticSearchExtensions
    {
        /// <summary>
        /// Adds a <see cref="ElasticSearchFilterProvider"/> with default configuration
        /// </summary>
        /// <param name="descriptor">The descriptor where the provider is registered</param>
        /// <returns>The descriptor that was passed in as a parameter</returns>
        public static IFilterConventionDescriptor UseElasticSearchProvider(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.Provider(new ElasticSearchFilterProvider(x => x.AddDefaultElasticSearchFieldHandlers()));

        /// <summary>
        /// Initializes the default configuration of the provider by registering handlers
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/> that was passed in as a parameter</returns>
        public static IFilterProviderDescriptor<ElasticSearchFilterVisitorContext>
            AddDefaultElasticSearchFieldHandlers(
                this IFilterProviderDescriptor<ElasticSearchFilterVisitorContext> descriptor)
        {
            descriptor.AddFieldHandler<ElasticSearchStringEqualsOperationHandler>();

            /*
            descriptor.AddFieldHandler<ElasticSearchNotEqualsOperationHandler>();

            descriptor.AddFieldHandler<ElasticSearchInOperationHandler>();
            descriptor.AddFieldHandler<ElasticSearchNotInOperationHandler>();

            descriptor.AddFieldHandler<ElasticSearchComparableGreaterThanHandler>();
            descriptor.AddFieldHandler<ElasticSearchComparableNotGreaterThanHandler>();
            descriptor.AddFieldHandler<ElasticSearchComparableGreaterThanOrEqualsHandler>();
            descriptor.AddFieldHandler<ElasticSearchComparableNotGreaterThanOrEqualsHandler>();
            descriptor.AddFieldHandler<ElasticSearchComparableLowerThanHandler>();
            descriptor.AddFieldHandler<ElasticSearchComparableNotLowerThanHandler>();
            descriptor.AddFieldHandler<ElasticSearchComparableLowerThanOrEqualsHandler>();
            descriptor.AddFieldHandler<ElasticSearchComparableNotLowerThanOrEqualsHandler>();

            descriptor.AddFieldHandler<ElasticSearchStringStartsWithHandler>();
            descriptor.AddFieldHandler<ElasticSearchStringNotStartsWithHandler>();
            descriptor.AddFieldHandler<ElasticSearchStringEndsWithHandler>();
            descriptor.AddFieldHandler<ElasticSearchStringNotEndsWithHandler>();
            descriptor.AddFieldHandler<ElasticSearchStringContainsHandler>();
            descriptor.AddFieldHandler<ElasticSearchStringNotContainsHandler>();

            descriptor.AddFieldHandler<ElasticSearchListAllOperationHandler>();
            descriptor.AddFieldHandler<ElasticSearchListAnyOperationHandler>();
            descriptor.AddFieldHandler<ElasticSearchListNoneOperationHandler>();
            descriptor.AddFieldHandler<ElasticSearchListSomeOperationHandler>();
            */

            descriptor.AddFieldHandler<ElasticSearchDefaultFieldHandler>();

            return descriptor;
        }
    }
}
