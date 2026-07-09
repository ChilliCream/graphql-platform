namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that query parameters and route parameters cannot conflict naming wise,
/// i.e. map to the same variable or input value field.
/// </summary>
internal sealed class EndpointParametersMustNotConflictRule : IOpenApiEndpointDefinitionValidationRule
{
    public OpenApiDefinitionValidationResult Validate(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context)
    {
        List<OpenApiDefinitionValidationError>? errors = null;

        var parameterMappings = new Dictionary<string, int>();

        if (endpoint.BodyVariableName is not null)
        {
            parameterMappings.Add(endpoint.BodyVariableName, 1);
        }

        foreach (var routeParam in endpoint.RouteParameters)
        {
            var key = GetParameterKey(routeParam);
            parameterMappings.TryGetValue(key, out var count);
            parameterMappings[key] = count + 1;
        }

        foreach (var queryParam in endpoint.QueryParameters)
        {
            var key = GetParameterKey(queryParam);
            parameterMappings.TryGetValue(key, out var count);
            parameterMappings[key] = count + 1;
        }

        foreach (var (key, count) in parameterMappings)
        {
            if (count > 1)
            {
                errors ??= [];
                errors.Add(new OpenApiDefinitionValidationError(
                    $"Endpoint has {count} parameters mapping to the same variable(-path) '${key}'. Each variable(-path) can only be mapped once.",
                    endpoint));
            }
        }

        return errors is not { Count: > 0 }
            ? OpenApiDefinitionValidationResult.Success()
            : OpenApiDefinitionValidationResult.Failure(errors);
    }

    private static string GetParameterKey(OpenApiEndpointDefinitionParameter parameter)
    {
        if (parameter.InputObjectPath is { Length: > 0 } inputObjectPath)
        {
            return $"{parameter.VariableName}.{string.Join(".", inputObjectPath)}";
        }

        return parameter.VariableName;
    }
}
