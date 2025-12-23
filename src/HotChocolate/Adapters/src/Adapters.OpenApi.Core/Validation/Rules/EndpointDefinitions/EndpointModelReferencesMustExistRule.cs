namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that models referenced by endpoint definitions must exist.
/// </summary>
internal sealed class EndpointModelReferencesMustExistRule : IOpenApiEndpointDefinitionValidationRule
{
    public async ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        var errors = new List<OpenApiDefinitionValidationError>();

        foreach (var modelName in endpoint.ExternalFragmentReferences)
        {
            var model = await context.GetModelAsync(modelName);

            if (model is null)
            {
                errors.Add(new OpenApiDefinitionValidationError(
                    $"Model '{modelName}' referenced by endpoint '{endpoint.OperationDefinition.Name!.Value}' does not exist.",
                    endpoint));
            }
        }

        return errors.Count == 0 ? OpenApiDefinitionValidationResult.Success() : OpenApiDefinitionValidationResult.Failure(errors);
    }
}
