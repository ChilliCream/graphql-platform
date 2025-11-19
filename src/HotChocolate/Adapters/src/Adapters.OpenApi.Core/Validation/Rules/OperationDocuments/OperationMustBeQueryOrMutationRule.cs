using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that operation documents can only be query or mutation (not subscription).
/// </summary>
internal sealed class OperationMustBeQueryOrMutationRule : IOpenApiOperationDocumentValidationRule
{
    public ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken)
    {
        var operationType = document.OperationDefinition.Operation;

        if (operationType is OperationType.Subscription)
        {
            return ValueTask.FromResult(OpenApiValidationResult.Failure(
                new OpenApiValidationError(
                    $"Operation '{document.Name}' is a subscription. Only queries and mutations are allowed for OpenAPI operations.",
                    document)));
        }

        return ValueTask.FromResult(OpenApiValidationResult.Success());
    }
}
