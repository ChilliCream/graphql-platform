using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

public static class EndpointRouteBuilderExtensions
{
    // TODO: Better name
    public static IEndpointRouteBuilder MapGraphQLEndpoints(
        this IEndpointRouteBuilder endpoints,
        string? schemaName = null)
    {
        var dataSource = endpoints.ServiceProvider.GetRequiredKeyedService<DynamicEndpointDataSource>(schemaName);

        if (!endpoints.DataSources.Contains(dataSource))
        {
            endpoints.DataSources.Add(dataSource);
        }

        return endpoints;
    }
}
