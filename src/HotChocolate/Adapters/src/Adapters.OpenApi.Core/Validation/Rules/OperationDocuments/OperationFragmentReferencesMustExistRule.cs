namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that fragments referenced by operation documents must exist.
/// </summary>
internal sealed class OperationFragmentReferencesMustExistRule : IOpenApiOperationDocumentValidationRule
{
    public async ValueTask<OpenApiDocumentValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiDocumentValidationContext context,
        CancellationToken cancellationToken)
    {
        var errors = new List<OpenApiDocumentValidationError>();

        foreach (var fragmentName in document.ExternalFragmentReferences)
        {
            var fragment = await context.GetFragmentAsync(fragmentName);

            if (fragment is null)
            {
                errors.Add(new OpenApiDocumentValidationError(
                    $"Fragment '{fragmentName}' referenced by operation document '{document.Name}' does not exist.",
                    document));
            }
        }

        return errors.Count == 0 ? OpenApiDocumentValidationResult.Success() : OpenApiDocumentValidationResult.Failure(errors);
    }
}
