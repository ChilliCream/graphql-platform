using System.Collections.Immutable;

namespace HotChocolate.Exporters.OpenApi.Validation;

internal sealed class OpenApiDocumentValidator : IOpenApiDocumentValidator
{
    private static readonly ImmutableArray<IOpenApiFragmentDocumentValidationRule> s_fragmentDocumentValidationRules =
    [
    ];

    private static readonly ImmutableArray<IOpenApiOperationDocumentValidationRule> s_operationDocumentValidationRules =
    [
    ];

    public async ValueTask ValidateAsync(
        IOpenApiDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken)
    {
        if (document is OpenApiOperationDocument operationDocument)
        {
            foreach (var rule in s_operationDocumentValidationRules)
            {
                await rule.ValidateAsync(operationDocument, context, cancellationToken).ConfigureAwait(false);
            }
        }
        else if (document is OpenApiFragmentDocument fragmentDocument)
        {
            foreach (var rule in s_fragmentDocumentValidationRules)
            {
                await rule.ValidateAsync(fragmentDocument, context, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            throw new NotSupportedException();
        }
    }
}
