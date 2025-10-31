namespace HotChocolate.Exporters.OpenApi.Validation;

public interface IOpenApiDocumentValidator
{
    ValueTask ValidateAsync(
        IOpenApiDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken);
}
