using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that endpoint definitions cannot contain @defer or @stream directives.
/// </summary>
internal sealed class EndpointNoDeferStreamDirectiveRule : IOpenApiEndpointDefinitionValidationRule
{
    private static readonly DeferStreamDirectiveFinder s_finder = new();

    public ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        var documentNode = CreateDocumentNode(endpoint);
        var finderContext = new DeferStreamDirectiveFinder.DeferStreamFinderContext();

        s_finder.Visit(documentNode, finderContext);

        if (finderContext.FoundDirective is not null)
        {
            return ValueTask.FromResult(OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Endpoint '{endpoint.OperationDefinition.Name!.Value}' contains the '@{finderContext.FoundDirective}' directive, which is not allowed in OpenAPI definitions.",
                    endpoint)));
        }

        return ValueTask.FromResult(OpenApiDefinitionValidationResult.Success());
    }

    private static DocumentNode CreateDocumentNode(OpenApiEndpointDefinition endpoint)
    {
        var definitions = new List<IDefinitionNode> { endpoint.OperationDefinition };
        definitions.AddRange(endpoint.LocalFragmentsByName.Values);
        return new DocumentNode(null, definitions.ToArray());
    }
}
