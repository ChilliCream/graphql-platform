namespace HotChocolate.Adapters.OpenApi;

internal interface IOpenApiFragmentDocumentValidationRule
{
    ValueTask<OpenApiDocumentValidationResult> ValidateAsync(
        OpenApiFragmentDocument document,
        IOpenApiDocumentValidationContext context,
        CancellationToken cancellationToken);
}
