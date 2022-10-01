using HotChocolate.Data.Marten.Sorting.Handlers;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Marten.Sorting.Extensions;

public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMartenSorting(this IRequestExecutorBuilder requestExecutorBuilder)
    {
        return requestExecutorBuilder
            .AddSorting(c => c
                .AddDefaultOperations()
                .BindDefaultTypes()
                .UseMartenQueryableSortProvider());
    }

    private static ISortConventionDescriptor UseMartenQueryableSortProvider(
        this ISortConventionDescriptor descriptor)
    {
        var queryableSortProvider = new QueryableSortProvider(c =>
            c.AddMartenFieldHandlers());
        descriptor.Provider(queryableSortProvider);
        return descriptor;
    }

    private static ISortProviderDescriptor<QueryableSortContext> AddMartenFieldHandlers(
        this ISortProviderDescriptor<QueryableSortContext> descriptor)
    {
        descriptor.AddOperationHandler<QueryableAscendingSortOperationHandler>();
        descriptor.AddOperationHandler<QueryableDescendingSortOperationHandler>();

        /*
         * Custom field handler that generates sorting expressions that
         * are digestible for the Marten LINQ provider.
         * See https://github.com/ChilliCream/hotchocolate/issues/5282 for more details.
         */
        descriptor.AddFieldHandler<MartenQueryableSortFieldHandler>();
        return descriptor;
    }
}
