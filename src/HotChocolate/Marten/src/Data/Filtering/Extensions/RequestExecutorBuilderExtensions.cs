using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Marten.Filtering.Handlers;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Marten.Filtering.Extensions;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMartenFiltering(this IRequestExecutorBuilder requestExecutorBuilder)
    {
        return requestExecutorBuilder
            .AddFiltering(c => c
                .AddDefaultOperations()
                .BindDefaultTypes()
                .UseMartenQueryableFilterProvider());
    }

    private static IFilterConventionDescriptor UseMartenQueryableFilterProvider(
        this IFilterConventionDescriptor descriptor)
    {
        var queryableFilterProvider = new QueryableFilterProvider(c =>
            c.AddMartenFieldHandlers());
        descriptor.Provider(queryableFilterProvider);
        return descriptor;
    }

    private static IFilterProviderDescriptor<QueryableFilterContext> AddMartenFieldHandlers(
        this IFilterProviderDescriptor<QueryableFilterContext> descriptor)
    {
        descriptor.AddFieldHandler<QueryableBooleanEqualsHandler>();
        descriptor.AddFieldHandler<QueryableBooleanNotEqualsHandler>();

        descriptor.AddFieldHandler<QueryableComparableEqualsHandler>();
        descriptor.AddFieldHandler<QueryableComparableNotEqualsHandler>();
        descriptor.AddFieldHandler<QueryableComparableInHandler>();
        descriptor.AddFieldHandler<QueryableComparableNotInHandler>();
        descriptor.AddFieldHandler<QueryableComparableGreaterThanHandler>();
        descriptor.AddFieldHandler<QueryableComparableNotGreaterThanHandler>();
        descriptor.AddFieldHandler<QueryableComparableGreaterThanOrEqualsHandler>();
        descriptor.AddFieldHandler<QueryableComparableNotGreaterThanOrEqualsHandler>();
        descriptor.AddFieldHandler<QueryableComparableLowerThanHandler>();
        descriptor.AddFieldHandler<QueryableComparableNotLowerThanHandler>();
        descriptor.AddFieldHandler<QueryableComparableLowerThanOrEqualsHandler>();
        descriptor.AddFieldHandler<QueryableComparableNotLowerThanOrEqualsHandler>();

        descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
        descriptor.AddFieldHandler<QueryableStringNotEqualsHandler>();
        descriptor.AddFieldHandler<QueryableStringStartsWithHandler>();
        descriptor.AddFieldHandler<QueryableStringNotStartsWithHandler>();
        descriptor.AddFieldHandler<QueryableStringEndsWithHandler>();
        descriptor.AddFieldHandler<QueryableStringNotEndsWithHandler>();
        descriptor.AddFieldHandler<QueryableStringInHandler>();
        descriptor.AddFieldHandler<QueryableStringNotInHandler>();
        descriptor.AddFieldHandler<QueryableStringContainsHandler>();
        descriptor.AddFieldHandler<QueryableStringNotContainsHandler>();

        descriptor.AddFieldHandler<QueryableEnumEqualsHandler>();
        descriptor.AddFieldHandler<QueryableEnumNotEqualsHandler>();
        descriptor.AddFieldHandler<QueryableEnumInHandler>();
        descriptor.AddFieldHandler<QueryableEnumNotInHandler>();

        descriptor.AddFieldHandler<QueryableListAnyOperationHandler>();
        descriptor.AddFieldHandler<QueryableListAllOperationHandler>();
        descriptor.AddFieldHandler<QueryableListNoneOperationHandler>();
        descriptor.AddFieldHandler<QueryableListSomeOperationHandler>();

        descriptor.AddFieldHandler<QueryableDataOperationHandler>();
        /*
         * Custom field handler that generates filtering expressions that
         * are digestible for the Marten LINQ provider.
         * See https://github.com/ChilliCream/hotchocolate/issues/5282 for more details.
         */
        descriptor.AddFieldHandler<MartenQueryableFieldHandler>();
        return descriptor;
    }
}
