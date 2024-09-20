namespace HotChocolate.AspNetCore.Instrumentation;

/// <summary>
/// Register an implementation of this interface in the DI container to
/// listen to transport diagnostic events. Multiple instances can be registered
/// and they will all be called in the registration order.
/// </summary>
/// <seealso cref="ServerDiagnosticEventListener"/>
public interface IServerDiagnosticEventListener : IServerDiagnosticEvents
{
}
