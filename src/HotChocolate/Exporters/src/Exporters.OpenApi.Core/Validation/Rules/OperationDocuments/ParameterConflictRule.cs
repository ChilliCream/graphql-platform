namespace HotChocolate.Exporters.OpenApi.Validation;

/// <summary>
/// Validates that query parameters and route parameters cannot conflict naming wise,
/// i.e. map to the same variable or input value field.
/// </summary>
internal sealed class ParameterConflictRule : IOpenApiOperationDocumentValidationRule
{
    public ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken)
    {
        var errors = new List<OpenApiValidationError>();

        // Collect all parameter mappings
        var parameterMappings = new Dictionary<string, List<string>>();

        // Route parameters
        foreach (var routeParam in document.Route.Parameters)
        {
            var key = GetParameterKey(routeParam);
            if (!parameterMappings.ContainsKey(key))
            {
                parameterMappings[key] = new List<string>();
            }

            parameterMappings[key].Add($"route parameter '{routeParam.Key}'");
        }

        // Query parameters
        foreach (var queryParam in document.QueryParameters)
        {
            var key = GetParameterKey(queryParam);
            if (!parameterMappings.ContainsKey(key))
            {
                parameterMappings[key] = new List<string>();
            }

            parameterMappings[key].Add($"query parameter '{queryParam.Key}'");
        }

        // Check for conflicts
        foreach (var (key, sources) in parameterMappings)
        {
            if (sources is { Count: > 1 })
            {
                errors.Add(new OpenApiValidationError(
                    $"Operation '{document.Name}' has conflicting parameters that map to '{key}': {string.Join(", ", sources)}.",
                    document.Id,
                    document.Name));
            }
        }

        return errors.Count == 0
            ? ValueTask.FromResult(OpenApiValidationResult.Success())
            : ValueTask.FromResult(OpenApiValidationResult.Failure(errors));
    }

    private static string GetParameterKey(OpenApiRouteSegmentParameter parameter)
    {
        if (parameter.InputObjectPath is { Length: > 0 } inputObjectPath)
        {
            return $"{parameter.Variable}.{string.Join(".", inputObjectPath)}";
        }

        return parameter.Variable;
    }
}
