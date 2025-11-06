using System.Collections.Immutable;

namespace HotChocolate.Exporters.OpenApi.Validation;

internal sealed class OpenApiDocumentValidator : IOpenApiDocumentValidator
{
    private static readonly ImmutableArray<IOpenApiFragmentDocumentValidationRule> s_fragmentDocumentValidationRules =
    [
        new FragmentNameUniquenessRule(),
        new FragmentReferencesMustExistRule()
    ];

    private static readonly ImmutableArray<IOpenApiOperationDocumentValidationRule> s_operationDocumentValidationRules =
    [
        new OperationNameUniquenessRule(),
        new OperationMustBeQueryOrMutationRule(),
        new OperationMustHaveSingleRootFieldRule(),
        new OperationFragmentReferencesMustExistRule(),
        new ParameterConflictRule(),
        new OperationMustCompileAgainstSchemaRule()
    ];

    public async ValueTask<OpenApiValidationResult> ValidateAsync(
        IOpenApiDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken)
    {
        if (document is OpenApiOperationDocument operationDocument)
        {
            foreach (var rule in s_operationDocumentValidationRules)
            {
                var result = await rule.ValidateAsync(operationDocument, context, cancellationToken).ConfigureAwait(false);
                if (!result.IsValid)
                {
                    return result;
                }
            }
        }
        else if (document is OpenApiFragmentDocument fragmentDocument)
        {
            foreach (var rule in s_fragmentDocumentValidationRules)
            {
                var result = await rule.ValidateAsync(fragmentDocument, context, cancellationToken).ConfigureAwait(false);
                if (!result.IsValid)
                {
                    return result;
                }
            }
        }
        else
        {
            throw new NotSupportedException();
        }

        return OpenApiValidationResult.Success();
    }
}
