using Microsoft.AspNetCore.Routing.Patterns;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that an endpoint has a valid route pattern.
/// </summary>
internal sealed class EndpointMustHaveValidRouteRule : IOpenApiEndpointDefinitionValidationRule
{
    public OpenApiDefinitionValidationResult Validate(OpenApiEndpointDefinition endpoint)
    {
        try
        {
            RoutePatternFactory.Parse(endpoint.Route);
            return OpenApiDefinitionValidationResult.Success();
        }
        catch (RoutePatternException)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Route pattern '{endpoint.Route}' is invalid.",
                    endpoint));
        }
    }
}
