namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that named fragments on an endpoint cannot contain the @hoist directive.
/// </summary>
internal sealed class EndpointNoHoistDirectiveInFragmentsRule : IOpenApiEndpointDefinitionValidationRule
{
    private static readonly HoistDirectiveFinder s_finder = new();

    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        var finderContext = new HoistDirectiveFinder.Context();

        foreach (var fragment in endpoint.LocalFragmentsByName.Values)
        {
            s_finder.Visit(fragment, finderContext);
        }

        if (finderContext.Count > 0)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "Endpoint named fragments cannot contain the '@hoist' directive.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
