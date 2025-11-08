namespace HotChocolate.Exporters.OpenApi.Validation;

/// <summary>
/// Validates that query parameters and route parameters cannot conflict naming wise,
/// i.e. map to the same variable or input value field.
/// </summary>
internal sealed class OperationParameterConflictRule : IOpenApiOperationDocumentValidationRule
{
    public ValueTask<OpenApiValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiValidationContext context,
        CancellationToken cancellationToken)
    {
        var errors = new List<OpenApiValidationError>();

        var parameterMappings = new Dictionary<string, List<string>>();

        foreach (var routeParam in document.Route.Parameters)
        {
            var key = GetParameterKey(routeParam);
            if (!parameterMappings.TryGetValue(key, out var value))
            {
                value = new List<string>();
                parameterMappings[key] = value;
            }

            value.Add($"route parameter '{routeParam.Key}'");
        }

        foreach (var queryParam in document.QueryParameters)
        {
            var key = GetParameterKey(queryParam);
            if (!parameterMappings.TryGetValue(key, out var value))
            {
                value = new List<string>();
                parameterMappings[key] = value;
            }

            value.Add($"query parameter '{queryParam.Key}'");
        }

        foreach (var (key, sources) in parameterMappings)
        {
            if (sources is { Count: > 1 })
            {
                errors.Add(new OpenApiValidationError(
                    $"Operation '{document.Name}' has conflicting parameters that map to '{key}': {string.Join(", ", sources)}.",
                    document));
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
