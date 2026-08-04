namespace Mocha.Mediator;

internal sealed class NoopMediatorDiagnosticEvents
    : MediatorDiagnosticEventListener
{
    private NoopMediatorDiagnosticEvents()
    {
    }

    public static NoopMediatorDiagnosticEvents Instance { get; } = new();
}
