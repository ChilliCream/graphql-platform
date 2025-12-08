using System.Collections.Immutable;
using HotChocolate.Adapters.OpenApi.Validation;

namespace HotChocolate.Adapters.OpenApi;

public sealed class OpenApiDocumentValidator
{
    private static readonly ImmutableArray<IOpenApiFragmentDocumentValidationRule> s_fragmentDocumentValidationRules =
    [
        new FragmentNameUniquenessRule(),
        new FragmentTypeConditionMustExistInSchemaRule(),
        new FragmentNoDeferStreamDirectiveRule(),
        new FragmentReferencesMustExistRule()
    ];

    private static readonly ImmutableArray<IOpenApiOperationDocumentValidationRule> s_operationDocumentValidationRules =
    [
        new OperationNameUniquenessRule(),
        new OperationMustBeQueryOrMutationRule(),
        new OperationMustHaveSingleRootFieldRule(),
        new OperationNoDeferStreamDirectiveRule(),
        new OperationFragmentReferencesMustExistRule(),
        new OperationParameterConflictRule(),
        new OperationRouteUniquenessRule(),
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
