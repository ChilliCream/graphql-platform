namespace GreenDonut;

/// <summary>
/// Register an implementation of this interface in the DI container to
/// listen to diagnostic events. Multiple implementations can be registered
/// and they will all be notified in the registration order.
/// </summary>
/// <seealso cref="DataLoaderDiagnosticEventListener"/>
public interface IDataLoaderDiagnosticEventListener : IDataLoaderDiagnosticEvents;
