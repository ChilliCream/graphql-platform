namespace HotChocolate.Exporters.OpenApi;

internal interface IOpenApiOperationDocumentValidationRule
{
    ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken);
}
