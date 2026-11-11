namespace Mocha.Hosting;

internal sealed class MessageBusHealthCheckOptions
{
    public Uri? Endpoint { get; init; }

    public required TimeSpan Timeout { get; init; }
}
