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
}
