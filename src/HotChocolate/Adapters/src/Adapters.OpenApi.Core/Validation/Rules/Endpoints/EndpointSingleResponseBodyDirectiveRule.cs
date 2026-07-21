namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates the @responseBody directive on an endpoint definition.
/// </summary>
internal sealed class EndpointSingleResponseBodyDirectiveRule
    : IOpenApiEndpointDefinitionValidationRule
{
    private static readonly ResponseBodyDirectiveFinder s_finder = new();

    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        var finderContext = new ResponseBodyDirectiveFinder.Context();

        s_finder.Visit(endpoint.OperationDefinition, finderContext);

        if (finderContext.Count > 1)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "Endpoint operations can contain at most one '@responseBody' directive.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
