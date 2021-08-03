using HotChocolate.Data.Filters;

namespace HotChocolate.Data.SqlKata.Filters
{
    public static class FilterConventionDescriptorSqlKataExtensions
    {
        /// <summary>
        /// Adds a <see cref="SqlKataFilterProvider"/> with default configuration
        /// </summary>
        /// <param name="descriptor">The descriptor where the provider is registered</param>
        /// <returns>The descriptor that was passed in as a parameter</returns>
        public static IFilterConventionDescriptor UseSqlKataProvider(
            this IFilterConventionDescriptor descriptor) =>
            descriptor.Provider(new SqlKataFilterProvider(x => x.AddDefaultSqlKataFieldHandlers()));

        /// <summary>
        /// Initializes the default configuration of the provider by registering handlers
        /// </summary>
        /// <param name="descriptor">The descriptor where the handlers are registered</param>
        /// <returns>The <paramref name="descriptor"/> that was passed in as a parameter</returns>
        public static IFilterProviderDescriptor<SqlKataFilterVisitorContext>
            AddDefaultSqlKataFieldHandlers(
                this IFilterProviderDescriptor<SqlKataFilterVisitorContext> descriptor)
        {
            descriptor.AddFieldHandler<SqlKataEqualsOperationHandler>();
            descriptor.AddFieldHandler<SqlKataNotEqualsOperationHandler>();

            descriptor.AddFieldHandler<SqlKataInOperationHandler>();
            descriptor.AddFieldHandler<SqlKataNotInOperationHandler>();

            descriptor.AddFieldHandler<SqlKataComparableGreaterThanHandler>();
            descriptor.AddFieldHandler<SqlKataComparableNotGreaterThanHandler>();
            descriptor.AddFieldHandler<SqlKataComparableGreaterThanOrEqualsHandler>();
            descriptor.AddFieldHandler<SqlKataComparableNotGreaterThanOrEqualsHandler>();
            descriptor.AddFieldHandler<SqlKataComparableLowerThanHandler>();
            descriptor.AddFieldHandler<SqlKataComparableNotLowerThanHandler>();
            descriptor.AddFieldHandler<SqlKataComparableLowerThanOrEqualsHandler>();
            descriptor.AddFieldHandler<SqlKataComparableNotLowerThanOrEqualsHandler>();

            descriptor.AddFieldHandler<SqlKataStringStartsWithHandler>();
            descriptor.AddFieldHandler<SqlKataStringNotStartsWithHandler>();
            descriptor.AddFieldHandler<SqlKataStringEndsWithHandler>();
            descriptor.AddFieldHandler<SqlKataStringNotEndsWithHandler>();
            descriptor.AddFieldHandler<SqlKataStringContainsHandler>();
            descriptor.AddFieldHandler<SqlKataStringNotContainsHandler>();

            descriptor.AddFieldHandler<SqlKataListAllOperationHandler>();
            descriptor.AddFieldHandler<SqlKataListAnyOperationHandler>();
            descriptor.AddFieldHandler<SqlKataListNoneOperationHandler>();
            descriptor.AddFieldHandler<SqlKataListSomeOperationHandler>();

            descriptor.AddFieldHandler<SqlKataDefaultFieldHandler>();

            return descriptor;
        }
    }
}
