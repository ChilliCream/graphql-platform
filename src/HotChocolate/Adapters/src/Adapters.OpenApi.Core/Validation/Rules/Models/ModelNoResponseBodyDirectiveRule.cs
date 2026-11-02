namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that a model definition cannot contain the @responseBody directive.
/// </summary>
internal sealed class ModelNoResponseBodyDirectiveRule : IOpenApiModelDefinitionValidationRule
{
    private static readonly ResponseBodyDirectiveFinder s_finder = new();

    public OpenApiDefinitionValidationResult Validate(
        OpenApiModelDefinition model,
        IOpenApiDefinitionValidationContext context)
    {
        var finderContext = new ResponseBodyDirectiveFinder.Context();

        s_finder.Visit(model.Document, finderContext);

        if (finderContext.Count > 0)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "OpenAPI models cannot contain the '@responseBody' directive.",
                    model));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
