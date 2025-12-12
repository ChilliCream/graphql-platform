namespace HotChocolate.Adapters.OpenApi;

internal interface IOpenApiOperationDocumentValidationRule
{
    ValueTask<OpenApiDocumentValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiDocumentValidationContext context,
        CancellationToken cancellationToken);
}
