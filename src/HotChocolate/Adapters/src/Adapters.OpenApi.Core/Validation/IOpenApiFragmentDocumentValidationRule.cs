namespace HotChocolate.Adapters.OpenApi;

internal interface IOpenApiFragmentDocumentValidationRule
{
    ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiFragmentDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken);
}
