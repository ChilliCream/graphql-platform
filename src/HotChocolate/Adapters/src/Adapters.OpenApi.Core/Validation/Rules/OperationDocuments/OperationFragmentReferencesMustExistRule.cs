namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that fragments referenced by operation documents must exist.
/// </summary>
internal sealed class OperationFragmentReferencesMustExistRule : IOpenApiOperationDocumentValidationRule
{
    public async ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken)
    {
        var errors = new List<OpenApiValidationError>();

        foreach (var fragmentName in document.ExternalFragmentReferences)
        {
            var fragment = await context.GetFragmentAsync(fragmentName);

            if (fragment is null)
            {
                errors.Add(new OpenApiValidationError(
                    $"Fragment '{fragmentName}' referenced by operation document '{document.Name}' does not exist.",
                    document));
            }
        }

        return errors.Count == 0 ? OpenApiValidationResult.Success() : OpenApiValidationResult.Failure(errors);
    }
}
