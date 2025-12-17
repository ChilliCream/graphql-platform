using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that an endpoint has a valid route pattern.
/// </summary>
internal sealed class EndpointMustHaveValidRouteRule : IOpenApiEndpointDefinitionValidationRule
{
    public ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        OpenApiDefinitionValidationResult result;
        try
        {
            RoutePatternFactory.Parse(endpoint.Route);

            result = OpenApiDefinitionValidationResult.Success();
        }
        catch (RoutePatternException)
        {
            result = OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Route pattern '{endpoint.Route}' is invalid.",
                    endpoint));
        }

        return ValueTask.FromResult(result);
    }
}
