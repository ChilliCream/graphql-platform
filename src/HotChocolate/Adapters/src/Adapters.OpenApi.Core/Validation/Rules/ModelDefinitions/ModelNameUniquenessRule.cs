namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that model names must be unique across all definitions.
/// </summary>
internal sealed class ModelNameUniquenessRule : IOpenApiModelDefinitionValidationRule
{
    public async ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiModelDefinition model,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        var existingModel = await context.GetModelAsync(model.Name);

        if (existingModel is null || existingModel == model)
        {
            return OpenApiDefinitionValidationResult.Success();
        }

        return OpenApiDefinitionValidationResult.Failure(
            new OpenApiDefinitionValidationError(
                $"Model name '{model.Name}' is already being used by another model definition ('{existingModel.Name}').",
                model));
    }
}
