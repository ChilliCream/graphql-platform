namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that operation document operation names must be unique across all documents.
/// </summary>
internal sealed class OperationNameUniquenessRule : IOpenApiOperationDocumentValidationRule
{
    public async ValueTask<OpenApiDocumentValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiDocumentValidationContext context,
        CancellationToken cancellationToken)
    {
        var existingOperation = await context.GetOperationAsync(document.Name);

        if (existingOperation is null || existingOperation.Id == document.Id)
        {
            return OpenApiDocumentValidationResult.Success();
        }

        return OpenApiDocumentValidationResult.Failure(
            new OpenApiDocumentValidationError(
                $"Operation name '{document.Name}' is already being used by a operation document with the Id '{existingOperation.Id}' ('{existingOperation.Name}').",
                document));
    }
}
