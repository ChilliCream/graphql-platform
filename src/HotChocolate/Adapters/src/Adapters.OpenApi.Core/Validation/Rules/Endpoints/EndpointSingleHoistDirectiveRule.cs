namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates the @hoist directive on an endpoint definition.
/// </summary>
internal sealed class EndpointSingleHoistDirectiveRule : IOpenApiEndpointDefinitionValidationRule
{
    private static readonly HoistDirectiveFinder s_finder = new();

    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        var finderContext = new HoistDirectiveFinder.Context();

        s_finder.Visit(endpoint.OperationDefinition, finderContext);

        if (finderContext.Count > 1)
        {
            return OpenApiDefinitionValidationResult.Failure(
                new OpenApiDefinitionValidationError(
                    "Endpoint must contain at most one '@hoist' directive.",
                    endpoint));
        }

        return OpenApiDefinitionValidationResult.Success();
    }
}
