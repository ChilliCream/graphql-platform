namespace HotChocolate.Exporters.OpenApi.Validation;

internal interface IOpenApiOperationDocumentValidationRule
{
    ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken);
}
