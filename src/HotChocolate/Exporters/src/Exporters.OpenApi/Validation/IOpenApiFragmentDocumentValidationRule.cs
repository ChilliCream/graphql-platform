namespace HotChocolate.Exporters.OpenApi.Validation;

internal interface IOpenApiFragmentDocumentValidationRule
{
    ValueTask ValidateAsync(
        OpenApiFragmentDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken);
}
