namespace HotChocolate.Exporters.OpenApi.Validation;

public interface IOpenApiDocumentValidator
{
    ValueTask<OpenApiValidationResult> ValidateAsync(
        IOpenApiDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken);
}
