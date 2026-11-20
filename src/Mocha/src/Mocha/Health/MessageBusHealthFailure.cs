namespace Mocha;

/// <summary>
/// Describes one unhealthy part of a message bus.
/// </summary>
/// <param name="Name">The name of the unhealthy part, for example the name of a transport.</param>
/// <param name="Reason">The reason the part is unhealthy.</param>
public readonly record struct MessageBusHealthFailure(string Name, string Reason);
