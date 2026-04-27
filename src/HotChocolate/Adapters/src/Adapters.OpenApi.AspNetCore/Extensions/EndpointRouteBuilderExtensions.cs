using HotChocolate.Execution;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapOpenApiEndpoints(
        this IEndpointRouteBuilder endpoints,
        string? schemaName = null)
    {
        TryResolveSchemaName(endpoints.ServiceProvider, ref schemaName);
        schemaName ??= ISchemaDefinition.DefaultName;

        var dataSource = endpoints.ServiceProvider.GetRequiredKeyedService<DynamicEndpointDataSource>(schemaName);

        if (!endpoints.DataSources.Contains(dataSource))
        {
            endpoints.DataSources.Add(dataSource);
        }

        return endpoints;
    }

    private static void TryResolveSchemaName(IServiceProvider services, ref string? schemaName)
    {
        if (schemaName is null
            && services.GetService<IRequestExecutorProvider>() is { } provider
            && provider.SchemaNames.Length == 1)
        {
            schemaName = provider.SchemaNames[0];
        }
    }
}
