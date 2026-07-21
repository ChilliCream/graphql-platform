namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that a model definition cannot contain the @hoist directive.
/// </summary>
internal sealed class ModelNoHoistDirectiveRule : IOpenApiModelDefinitionValidationRule
{
    private static readonly HoistDirectiveFinder s_finder = new();

    public OpenApiDefinitionValidationResult Validate(
        OpenApiModelDefinition model,
        IOpenApiDefinitionValidationContext context)
    {
        var finderContext = new HoistDirectiveFinder.Context();

        s_finder.Visit(model.Document, finderContext);

        if (finderContext.Count > 0)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "Model contains the '@hoist' directive, which is not supported for OpenAPI models.",
                    model));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
