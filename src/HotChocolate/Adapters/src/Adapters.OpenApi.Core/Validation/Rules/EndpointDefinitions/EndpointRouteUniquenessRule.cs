namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that route patterns combined with HTTP methods must be unique across all endpoints.
/// If a route pattern and HTTP method combination already exists, the endpoint must be the same.
/// </summary>
internal sealed class EndpointRouteUniquenessRule : IOpenApiEndpointDefinitionValidationRule
{
    public async ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        var existingEndpoint = await context.GetEndpointByRouteAndMethodAsync(endpoint.Route, endpoint.HttpMethod);

        if (existingEndpoint is not null && existingEndpoint != endpoint)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Route pattern '{endpoint.Route}' with HTTP method '{endpoint.HttpMethod}' is already being used by endpoint '{existingEndpoint.OperationDefinition.Name!.Value}'.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
