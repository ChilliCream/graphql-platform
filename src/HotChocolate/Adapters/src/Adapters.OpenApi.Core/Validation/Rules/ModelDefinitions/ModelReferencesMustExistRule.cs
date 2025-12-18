namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that fragments referenced by model definitions must exist.
/// </summary>
internal sealed class ModelReferencesMustExistRule : IOpenApiModelDefinitionValidationRule
{
    public async ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiModelDefinition model,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        var errors = new List<OpenApiDefinitionValidationError>();

        foreach (var fragmentName in model.ExternalFragmentReferences)
        {
            var fragment = await context.GetModelAsync(fragmentName);

            if (fragment is null)
            {
                errors.Add(new OpenApiDefinitionValidationError(
                    $"Model '{fragmentName}' referenced by model '{model.Name}' does not exist.",
                    model));
            }
        }

        return errors.Count == 0 ? OpenApiDefinitionValidationResult.Success() : OpenApiDefinitionValidationResult.Failure(errors);
    }
}
