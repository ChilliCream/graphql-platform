using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public static class MartenSortingRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMartenSorting(
        this IRequestExecutorBuilder requestExecutorBuilder)
    {
        return requestExecutorBuilder
            .ConfigureSchema(x => x.AddMartenSorting());
    }
}
