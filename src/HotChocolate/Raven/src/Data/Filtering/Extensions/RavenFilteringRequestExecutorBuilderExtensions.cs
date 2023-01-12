using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public static class RavenFilteringRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddRavenFiltering(
        this IRequestExecutorBuilder requestExecutorBuilder)
        => requestExecutorBuilder.ConfigureSchema(x => x.AddRavenFiltering());
}
