using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that model definitions cannot contain @defer or @stream directives.
/// </summary>
internal sealed class ModelNoDeferStreamDirectiveRule : IOpenApiModelDefinitionValidationRule
{
    private static readonly DeferStreamDirectiveFinder s_finder = new();

    public OpenApiDefinitionValidationResult Validate(OpenApiModelDefinition model)
    {
        var documentNode = CreateDocumentNode(model);
        var finderContext = new DeferStreamDirectiveFinder.DeferStreamFinderContext();

        s_finder.Visit(documentNode, finderContext);

        if (finderContext.FoundDirective is not null)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Model '{model.Name}' contains the '@{finderContext.FoundDirective}' directive, which is not allowed in OpenAPI definitions.",
                    model));
        }

        return OpenApiDefinitionValidationResult.Success();
    }

    private static DocumentNode CreateDocumentNode(OpenApiModelDefinition model)
    {
        var definitions = new List<IDefinitionNode> { model.FragmentDefinition };
        definitions.AddRange(model.LocalFragmentsByName.Values);
        return new DocumentNode(null, definitions.ToArray());
    }
}
