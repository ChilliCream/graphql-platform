namespace HotChocolate.Adapters.OpenApi.Validation;

/// <summary>
/// Validates that route patterns combined with HTTP methods must be unique across all operations.
/// If a route pattern and HTTP method combination already exists, the document ID must match.
/// </summary>
internal sealed class OperationRouteUniquenessRule : IOpenApiOperationDocumentValidationRule
{
    public async ValueTask<OpenApiDocumentValidationResult> ValidateAsync(
        OpenApiOperationDocument document,
        IOpenApiDocumentValidationContext context,
        CancellationToken cancellationToken)
    {
        var routePattern = document.Route.ToOpenApiPath();
        var existingOperation = await context.GetOperationByRouteAndMethodAsync(routePattern, document.HttpMethod);

        if (existingOperation is not null && existingOperation.Id != document.Id)
        {
            return OpenApiDocumentValidationResult.Failure(
                new OpenApiDocumentValidationError(
                    $"Route pattern '{routePattern}' with HTTP method '{document.HttpMethod}' is already being used by operation document with Id '{existingOperation.Id}'.",
                    document));
        }

        return OpenApiDocumentValidationResult.Success();
    }
}
