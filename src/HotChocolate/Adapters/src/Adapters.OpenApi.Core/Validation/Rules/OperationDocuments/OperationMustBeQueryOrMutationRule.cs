using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that operation documents can only be query or mutation (not subscription).
/// </summary>
internal sealed class OperationMustBeQueryOrMutationRule : IOpenApiOperationDocumentValidationRule
{
    public ValueTask<OpenApiDocumentValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiDocumentValidationContext context,
        CancellationToken cancellationToken)
    {
        var operationType = document.OperationDefinition.Operation;

        if (operationType is OperationType.Subscription)
        {
            return ValueTask.FromResult(OpenApiDocumentValidationResult.Failure(
                new OpenApiDocumentValidationError(
                    $"Operation '{document.Name}' is a subscription. Only queries and mutations are allowed for OpenAPI operations.",
                    document)));
        }

        return ValueTask.FromResult(OpenApiDocumentValidationResult.Success());
    }
}
