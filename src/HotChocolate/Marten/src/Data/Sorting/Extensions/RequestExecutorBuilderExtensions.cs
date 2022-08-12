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
        descriptor.AddOperationHandler<MartenQueryableAscendingSortOperationHandler>();
        descriptor.AddOperationHandler<MartenQueryableDescendingSortOperationHandler>();
        descriptor.AddFieldHandler<QueryableDefaultSortFieldHandler>();
        return descriptor;
    }
}
