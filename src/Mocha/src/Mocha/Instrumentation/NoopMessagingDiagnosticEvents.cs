namespace Mocha;

internal sealed class NoopMessagingDiagnosticEvents
    : MessagingDiagnosticEventListener
{
    private NoopMessagingDiagnosticEvents()
    {
    }

    public static NoopMessagingDiagnosticEvents Instance { get; } = new();
}
