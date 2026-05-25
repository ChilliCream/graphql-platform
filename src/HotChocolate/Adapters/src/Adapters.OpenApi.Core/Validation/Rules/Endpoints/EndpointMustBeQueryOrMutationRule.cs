using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that an endpoint definition can only be query or mutation (not subscription).
/// </summary>
internal sealed class EndpointMustBeQueryOrMutationRule : IOpenApiEndpointDefinitionValidationRule
{
    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        var operationType = endpoint.OperationDefinition.Operation;

        if (operationType is OperationType.Subscription)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "Endpoint operation type is 'subscription', but only 'query' and 'mutation' are supported.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
