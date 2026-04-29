using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

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

    private static void TryResolveSchemaName(OpenApiManager manager, ref string? schemaName)
    {
        if (schemaName is null && manager.Names.Length == 1)
        {
            schemaName = manager.Names[0];
        }
    }
}
