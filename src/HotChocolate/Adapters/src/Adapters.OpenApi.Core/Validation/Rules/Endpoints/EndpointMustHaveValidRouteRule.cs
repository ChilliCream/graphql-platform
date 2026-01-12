using System.Text.RegularExpressions;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that an endpoint definition has a valid route pattern.
/// </summary>
internal sealed partial class EndpointMustHaveValidRouteRule : IOpenApiEndpointDefinitionValidationRule
{
    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        var route = endpoint.Route;
        var regex = UrlPathRegex();

        if (string.IsNullOrEmpty(route) || !regex.IsMatch(route))
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Endpoint has invalid route pattern '{endpoint.Route}'.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }

    [GeneratedRegex(@"^/(?:(?:[a-zA-Z0-9_.-]+|\{[a-zA-Z0-9_]+\})(?:/(?:[a-zA-Z0-9_.-]+|\{[a-zA-Z0-9_]+\}))*)\z", RegexOptions.Compiled)]
    private static partial Regex UrlPathRegex();
}
