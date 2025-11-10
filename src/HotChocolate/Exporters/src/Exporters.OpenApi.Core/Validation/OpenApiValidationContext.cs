using HotChocolate.Validation;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class OpenApiValidationContext(
    IReadOnlyCollection<OpenApiOperationDocument> operations,
    IReadOnlyCollection<OpenApiFragmentDocument> fragments,
    ISchemaDefinition schema,
    DocumentValidator documentValidator) : IOpenApiValidationContext
{
    public ISchemaDefinition Schema { get; } = schema;

    public DocumentValidator DocumentValidator { get; } = documentValidator;

    public Dictionary<string, OpenApiOperationDocument> OperationsByName { get; } =
        operations.ToDictionary(o => o.Name, StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, OpenApiFragmentDocument> FragmentsByName { get; } =
        fragments.ToDictionary(o => o.Name, StringComparer.OrdinalIgnoreCase);

    public ValueTask<OpenApiFragmentDocument?> GetFragmentAsync(string fragmentName)
    {
        FragmentsByName.TryGetValue(fragmentName, out var document);

        return ValueTask.FromResult(document);
    }

    public ValueTask<OpenApiOperationDocument?> GetOperationAsync(string operationName)
    {
        OperationsByName.TryGetValue(operationName, out var document);

        return ValueTask.FromResult(document);
    }

    public ValueTask<OpenApiOperationDocument?> GetOperationByRouteAndMethodAsync(string routePattern, string httpMethod)
    {
        foreach (var operation in OperationsByName.Values)
        {
            var operationRoutePattern = operation.Route.ToOpenApiPath();
            if (string.Equals(routePattern, operationRoutePattern, StringComparison.OrdinalIgnoreCase)
                && string.Equals(httpMethod, operation.HttpMethod, StringComparison.OrdinalIgnoreCase))
            {
                return ValueTask.FromResult<OpenApiOperationDocument?>(operation);
            }
        }

        return ValueTask.FromResult<OpenApiOperationDocument?>(null);
    }
}
