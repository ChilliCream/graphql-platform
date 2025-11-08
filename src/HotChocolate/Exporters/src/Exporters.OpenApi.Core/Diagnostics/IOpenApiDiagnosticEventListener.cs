namespace HotChocolate.Exporters.OpenApi;

/// <summary>
/// Register an implementation of this interface in the DI container to
/// listen to diagnostic events. Multiple implementations can be registered,
/// and they will all be called in the registration order.
/// </summary>
/// <seealso cref="OpenApiDiagnosticEventListener"/>
public interface IOpenApiDiagnosticEventListener : IOpenApiDiagnosticEvents;
