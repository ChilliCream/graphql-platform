using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that endpoint definitions can only be query or mutation (not subscription).
/// </summary>
internal sealed class EndpointMustBeQueryOrMutationRule : IOpenApiEndpointDefinitionValidationRule
{
    public ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        var operationType = endpoint.OperationDefinition.Operation;

        if (operationType is OperationType.Subscription)
        {
            return ValueTask.FromResult(OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Endpoint '{endpoint.OperationDefinition.Name!.Value}' is a subscription. Only queries and mutations are allowed for OpenAPI endpoints.",
                    endpoint)));
        }

        return ValueTask.FromResult(OpenApiDefinitionValidationResult.Success());
    }
}
