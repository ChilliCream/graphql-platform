namespace Mocha.Mediator;

/// <summary>
/// Describes a configured mediator, including the service it is scoped to and its registered handlers,
/// for diagnostic and visualization purposes.
/// </summary>
/// <param name="Id">The stable URN identity of the mediator.</param>
/// <param name="ServiceName">The service name the mediator is scoped to.</param>
/// <param name="Handlers">The registered handlers.</param>
public sealed record MediatorDescription(
    string Id,
    string ServiceName,
    IReadOnlyList<MediatorHandlerDescription> Handlers);
