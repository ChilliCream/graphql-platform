using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public static class MartenFilteringRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddMartenFiltering(
        this IRequestExecutorBuilder requestExecutorBuilder)
    {
        return requestExecutorBuilder
            .ConfigureSchema(x => x.AddMartenFiltering());
    }
}
