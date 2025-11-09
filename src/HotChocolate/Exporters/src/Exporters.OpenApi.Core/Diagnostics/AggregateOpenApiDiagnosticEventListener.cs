namespace HotChocolate.Exporters.OpenApi;

internal sealed class AggregateOpenApiDiagnosticEventListener(IOpenApiDiagnosticEventListener[] listeners)
    : IOpenApiDiagnosticEventListener
{
    public void ValidationErrors(IReadOnlyList<IOpenApiError> errors)
    {
        foreach (var listener in listeners)
        {
            listener.ValidationErrors(errors);
        }
    }
}
