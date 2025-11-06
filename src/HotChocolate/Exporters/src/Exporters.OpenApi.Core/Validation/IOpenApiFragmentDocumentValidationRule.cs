namespace HotChocolate.Exporters.OpenApi.Validation;

internal interface IOpenApiFragmentDocumentValidationRule
{
    ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiFragmentDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken);
}
