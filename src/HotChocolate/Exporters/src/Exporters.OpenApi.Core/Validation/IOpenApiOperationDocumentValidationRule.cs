namespace HotChocolate.Exporters.OpenApi.Validation;

internal interface IOpenApiOperationDocumentValidationRule
{
    ValueTask ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken);
}
