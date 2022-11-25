namespace HotChocolate.Subscriptions.Diagnostics;

/// <summary>
/// This sealed event listener is used when no real listener has been registered.
/// </summary>
internal sealed class NoopSubscriptionDiagnosticEventsListener
    : SubscriptionDiagnosticEventsListener
{
    internal static NoopSubscriptionDiagnosticEventsListener Default { get; } = new();
}
