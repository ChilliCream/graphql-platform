using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that endpoint definitions can only be query or mutation (not subscription).
/// </summary>
internal sealed class EndpointMustBeQueryOrMutationRule : IOpenApiEndpointDefinitionValidationRule
{
    public OpenApiDefinitionValidationResult Validate(OpenApiEndpointDefinition endpoint)
    {
        var operationType = endpoint.OperationDefinition.Operation;

        if (operationType is OperationType.Subscription)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Endpoint '{endpoint.OperationDefinition.Name!.Value}' is a subscription. Only queries and mutations are allowed for OpenAPI endpoints.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
