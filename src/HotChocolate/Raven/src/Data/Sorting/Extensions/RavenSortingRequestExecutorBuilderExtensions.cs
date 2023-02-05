using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public static class RavenSortingRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddRavenSorting(
        this IRequestExecutorBuilder requestExecutorBuilder)
    {
        return requestExecutorBuilder
            .ConfigureSchema(x => x.AddRavenSorting());
    }
}
