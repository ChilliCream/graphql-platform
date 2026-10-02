namespace Mocha.Transport.Nats.Tests;

/// <summary>
/// Builds a standalone topology for tests that exercise topology resources without a running bus.
/// </summary>
internal static class TestTopology
{
    public static NatsMessagingTopology Create(NatsBusDefaults? defaults = null) => new(
        new NatsMessagingTransport(static _ => { }),
        new Uri("nats://localhost:4222/"),
        defaults ?? new NatsBusDefaults(),
        autoProvision: true);
}
