using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.AspNetCore.ServerDefaults;

#if NET8_0_OR_GREATER
namespace Microsoft.Extensions.Hosting;

public static class HotChocolateAspNetCoreHostingBuilderExtensions
{
    public static IRequestExecutorBuilder AddGraphQL(
        IHostApplicationBuilder builder,        
        string? schemaName = default,
        int maxAllowedRequestSize = MaxAllowedRequestSize)
        => builder.Services.AddGraphQLServer(schemaName, maxAllowedRequestSize);
}
#endif