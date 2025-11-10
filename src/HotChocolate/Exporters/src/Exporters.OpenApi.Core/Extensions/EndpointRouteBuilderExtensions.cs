using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapOpenApiEndpoints(
        this IEndpointRouteBuilder endpoints,
        string? schemaName = null)
    {
        schemaName ??= ISchemaDefinition.DefaultName;

        var dataSource = endpoints.ServiceProvider.GetRequiredKeyedService<DynamicEndpointDataSource>(schemaName);

        if (!endpoints.DataSources.Contains(dataSource))
        {
            endpoints.DataSources.Add(dataSource);
        }

        return endpoints;
    }
}
