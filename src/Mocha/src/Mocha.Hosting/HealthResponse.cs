namespace Mocha.Hosting;

/// <summary>
/// Represents the response to a <see cref="HealthRequest"/>, carrying a status message indicating the health of the message bus.
/// </summary>
/// <param name="Message">The health status message; a value of "OK" indicates a healthy bus.</param>
public sealed record HealthResponse(string Message);
