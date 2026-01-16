using Microsoft.AspNetCore.Http;

namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that an endpoint definition has a valid HTTP method.
/// </summary>
internal sealed class EndpointHttpMethodMustBeValidRule : IOpenApiEndpointDefinitionValidationRule
{
    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        if (!IsValidHttpMethod(endpoint.HttpMethod))
        {
            var method = string.IsNullOrEmpty(endpoint.HttpMethod) ? "(empty)" : endpoint.HttpMethod;
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    $"Endpoint has invalid HTTP method '{method}'. Allowed methods are GET, POST, PUT, PATCH, and DELETE.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }

    public static bool IsValidHttpMethod(string httpMethod)
    {
        if (string.IsNullOrEmpty(httpMethod))
        {
            return false;
        }

        return httpMethod == HttpMethods.Get
            || httpMethod == HttpMethods.Post
            || httpMethod == HttpMethods.Put
            || httpMethod == HttpMethods.Patch
            || httpMethod == HttpMethods.Delete;
    }
}
