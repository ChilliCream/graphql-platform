using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that operation documents cannot contain @defer or @stream directives.
/// </summary>
internal sealed class OperationNoDeferStreamDirectiveRule : IOpenApiOperationDocumentValidationRule
{
    private static readonly DeferStreamDirectiveFinder s_finder = new();

    public ValueTask<OpenApiDocumentValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiDocumentValidationContext context,
        CancellationToken cancellationToken)
    {
        var documentNode = CreateDocumentNode(document);
        var finderContext = new DeferStreamDirectiveFinder.DeferStreamFinderContext();

        s_finder.Visit(documentNode, finderContext);

        if (finderContext.FoundDirective is not null)
        {
            return ValueTask.FromResult(OpenApiDocumentValidationResult.Failure(
                new OpenApiDocumentValidationError(
                    $"Operation document '{document.Name}' contains the '@{finderContext.FoundDirective}' directive, which is not allowed in OpenAPI documents.",
                    document)));
        }

        return ValueTask.FromResult(OpenApiDocumentValidationResult.Success());
    }

    private static DocumentNode CreateDocumentNode(OpenApiOperationDocument document)
    {
        var definitions = new List<IDefinitionNode> { document.OperationDefinition };
        definitions.AddRange(document.LocalFragmentLookup.Values);
        return new DocumentNode(null, definitions.ToArray());
    }
}
