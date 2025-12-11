using System.Collections.Immutable;
using HotChocolate.Validation;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class OpenApiDocumentValidationContext(
    IImmutableDictionary<string, OpenApiOperationDocument> operationsById,
    IImmutableDictionary<string, OpenApiFragmentDocument> fragmentsById,
    ISchemaDefinition schema,
    DocumentValidator documentValidator) : IOpenApiDocumentValidationContext
{
    public ISchemaDefinition Schema { get; } = schema;

    public DocumentValidator DocumentValidator { get; } = documentValidator;

    public Dictionary<string, OpenApiOperationDocument> OperationsByName { get; } =
        operationsById.ToDictionary(
            o => o.Value.Name,
            o => o.Value,
            StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, OpenApiFragmentDocument> FragmentsByName { get; } =
        fragmentsById.ToDictionary(
            f => f.Value.Name,
            f => f.Value,
            StringComparer.OrdinalIgnoreCase);

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
