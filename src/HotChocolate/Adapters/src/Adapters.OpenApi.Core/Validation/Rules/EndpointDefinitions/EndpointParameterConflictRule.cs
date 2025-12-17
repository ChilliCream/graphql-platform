namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that query parameters and route parameters cannot conflict naming wise,
/// i.e. map to the same variable or input value field.
/// </summary>
internal sealed class EndpointParameterConflictRule : IOpenApiEndpointDefinitionValidationRule
{
    public ValueTask<OpenApiDefinitionValidationResult> ValidateAsync(
        OpenApiEndpointDefinition endpoint,
        IOpenApiDefinitionValidationContext context,
        CancellationToken cancellationToken)
    {
        var errors = new List<OpenApiDefinitionValidationError>();

        var parameterMappings = new Dictionary<string, int>();

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
                errors.Add(new OpenApiDefinitionValidationError(
                    $"Endpoint '{endpoint.OperationDefinition.Name!.Value}' has conflicting parameters that map to '${key}'.",
                    endpoint));
            }
        }

        return errors.Count == 0
            ? ValueTask.FromResult(OpenApiDefinitionValidationResult.Success())
            : ValueTask.FromResult(OpenApiDefinitionValidationResult.Failure(errors));
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
