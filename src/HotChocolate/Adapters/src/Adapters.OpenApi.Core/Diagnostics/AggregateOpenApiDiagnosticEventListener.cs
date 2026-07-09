namespace HotChocolate.Adapters.OpenApi;

internal sealed class AggregateOpenApiDiagnosticEventListener(IOpenApiDiagnosticEventListener[] listeners)
    : IOpenApiDiagnosticEventListener
{
    public void ValidationErrors(IReadOnlyList<OpenApiDefinitionValidationError> errors)
    {
        foreach (var listener in listeners)
        {
            listener.ValidationErrors(errors);
        }
    }
}
