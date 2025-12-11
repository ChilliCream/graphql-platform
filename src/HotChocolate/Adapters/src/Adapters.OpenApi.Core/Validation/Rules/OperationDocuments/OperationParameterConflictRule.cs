namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that query parameters and route parameters cannot conflict naming wise,
/// i.e. map to the same variable or input value field.
/// </summary>
internal sealed class OperationParameterConflictRule : IOpenApiOperationDocumentValidationRule
{
    public ValueTask<OpenApiDocumentValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiDocumentValidationContext context,
        CancellationToken cancellationToken)
    {
        var errors = new List<OpenApiDocumentValidationError>();

        var parameterMappings = new Dictionary<string, int>();

        foreach (var routeParam in document.Route.Parameters)
        {
            var key = GetParameterKey(routeParam);
            parameterMappings.TryGetValue(key, out var count);
            parameterMappings[key] = count + 1;
        }

        foreach (var queryParam in document.QueryParameters)
        {
            var key = GetParameterKey(queryParam);
            parameterMappings.TryGetValue(key, out var count);
            parameterMappings[key] = count + 1;
        }

        foreach (var (key, count) in parameterMappings)
        {
            if (count > 1)
            {
                errors.Add(new OpenApiDocumentValidationError(
                    $"Operation '{document.Name}' has conflicting parameters that map to '${key}'.",
                    document));
            }
        }

        return errors.Count == 0
            ? ValueTask.FromResult(OpenApiDocumentValidationResult.Success())
            : ValueTask.FromResult(OpenApiDocumentValidationResult.Failure(errors));
    }

    private static string GetParameterKey(OpenApiRouteSegmentParameter parameter)
    {
        if (parameter.InputObjectPath is { Length: > 0 } inputObjectPath)
        {
            return $"{parameter.VariableName}.{string.Join(".", inputObjectPath)}";
        }

        return parameter.VariableName;
    }
}
