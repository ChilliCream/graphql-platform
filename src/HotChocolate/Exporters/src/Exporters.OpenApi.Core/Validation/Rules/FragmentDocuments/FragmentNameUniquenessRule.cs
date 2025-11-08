namespace HotChocolate.Exporters.OpenApi.Validation;

/// <summary>
/// Validates that fragment names must be unique across all documents.
/// </summary>
internal sealed class FragmentNameUniquenessRule : IOpenApiFragmentDocumentValidationRule
{
    public async ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiFragmentDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken)
    {
        var existingFragment = await context.GetFragmentAsync(document.Name);

        if (existingFragment is null || existingFragment.Id == document.Id)
        {
            return OpenApiValidationResult.Success();
        }

        return OpenApiValidationResult.Failure(
            new OpenApiValidationError(
                $"Fragment name '{document.Name}' is already being used by a fragment document with the Id '{existingFragment.Id}' ('{existingFragment.Name}').",
                document));
    }
}
