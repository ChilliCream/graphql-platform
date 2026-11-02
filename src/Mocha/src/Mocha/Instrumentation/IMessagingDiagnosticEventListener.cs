namespace Mocha;

/// <summary>
/// Register an implementation of this interface in the DI container to
/// listen to diagnostic events. Multiple implementations can be registered
/// and they will all be called in registration order.
/// </summary>
/// <seealso cref="MessagingDiagnosticEventListener"/>
public interface IMessagingDiagnosticEventListener : IMessagingDiagnosticEvents;
