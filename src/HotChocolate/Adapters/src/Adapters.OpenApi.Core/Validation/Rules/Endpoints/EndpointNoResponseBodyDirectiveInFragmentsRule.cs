namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that named fragments on an endpoint cannot contain the @responseBody directive.
/// </summary>
internal sealed class EndpointNoResponseBodyDirectiveInFragmentsRule
    : IOpenApiEndpointDefinitionValidationRule
{
    private static readonly ResponseBodyDirectiveFinder s_finder = new();

    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        var finderContext = new ResponseBodyDirectiveFinder.Context();

        foreach (var fragment in endpoint.LocalFragmentsByName.Values)
        {
            s_finder.Visit(fragment, finderContext);
        }

        if (finderContext.Count > 0)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "Endpoint named fragments cannot contain the '@responseBody' directive.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
