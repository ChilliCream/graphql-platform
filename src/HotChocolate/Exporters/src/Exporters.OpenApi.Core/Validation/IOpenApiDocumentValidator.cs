namespace HotChocolate.Exporters.OpenApi;

internal interface IOpenApiDocumentValidator
{
    ValueTask<OpenApiValidationResult> ValidateAsync(
        IOpenApiDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken);
}
