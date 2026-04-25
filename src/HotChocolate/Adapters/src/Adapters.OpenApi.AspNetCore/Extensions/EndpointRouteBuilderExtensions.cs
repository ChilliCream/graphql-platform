using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapOpenApiEndpoints(
        this IEndpointRouteBuilder endpoints,
        string? schemaName = null)
    {
        var manager = endpoints.ServiceProvider.GetRequiredService<OpenApiManager>();

        TryResolveSchemaName(manager, ref schemaName);
        schemaName ??= ISchemaDefinition.DefaultName;

        var dataSource = (DynamicEndpointDataSource)manager.Get(schemaName).EndpointDataSource;

        if (!endpoints.DataSources.Contains(dataSource))
        {
            endpoints.DataSources.Add(dataSource);
        }

        return endpoints;
    }

    private static void TryResolveSchemaName(IOpenApiProvider provider, ref string? schemaName)
    {
        if (schemaName is null && provider.Names.Length == 1)
        {
            schemaName = provider.Names[0];
        }
    }
}
