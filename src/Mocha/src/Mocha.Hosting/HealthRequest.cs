namespace Mocha.Hosting;

/// <summary>
/// Represents a health check request sent through the message bus, expecting a <see cref="HealthResponse"/>.
/// </summary>
/// <param name="Message">A descriptive label for the health check request.</param>
public sealed record HealthRequest(string Message) : IEventRequest<HealthResponse>;
