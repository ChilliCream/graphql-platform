using HotChocolate.Data.Filters;
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
        var queryableFilterProvider = new MartenQueryableFilterProvider(c =>
            c.AddDefaultFieldHandlers());
        descriptor.Provider(queryableFilterProvider);
        return descriptor;
    }
}
