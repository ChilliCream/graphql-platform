using System.Collections.Immutable;
using HotChocolate.Validation;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class OpenApiDefinitionValidationContext(
    IImmutableDictionary<string, OpenApiEndpointDefinition> endpointsById,
    IImmutableDictionary<string, OpenApiModelDefinition> modelsById,
    ISchemaDefinition schema,
    DocumentValidator documentValidator) : IOpenApiDefinitionValidationContext
{
    public ISchemaDefinition Schema { get; } = schema;

    public DocumentValidator DocumentValidator { get; } = documentValidator;

    public Dictionary<string, OpenApiEndpointDefinition> EndpointsByName { get; } =
        endpointsById.ToDictionary(
            o => o.Value.OperationDefinition.Name!.Value,
            o => o.Value,
            StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, OpenApiModelDefinition> ModelsByName { get; } =
        modelsById.ToDictionary(
            f => f.Value.Name,
            f => f.Value,
            StringComparer.OrdinalIgnoreCase);

    public ValueTask<OpenApiModelDefinition?> GetModelAsync(string modelName)
    {
        ModelsByName.TryGetValue(modelName, out var model);

        return ValueTask.FromResult(model);
    }

    public ValueTask<OpenApiEndpointDefinition?> GetEndpointAsync(string endpointName)
    {
        EndpointsByName.TryGetValue(endpointName, out var endpoint);

        return ValueTask.FromResult(endpoint);
    }

    public ValueTask<OpenApiEndpointDefinition?> GetEndpointByRouteAndMethodAsync(string routePattern, string httpMethod)
    {
        foreach (var endpoint in EndpointsByName.Values)
        {
            if (string.Equals(routePattern, endpoint.Route, StringComparison.OrdinalIgnoreCase)
                && string.Equals(httpMethod, endpoint.HttpMethod, StringComparison.OrdinalIgnoreCase))
            {
                return ValueTask.FromResult<OpenApiEndpointDefinition?>(endpoint);
            }
        }

        return ValueTask.FromResult<OpenApiEndpointDefinition?>(null);
    }
}
