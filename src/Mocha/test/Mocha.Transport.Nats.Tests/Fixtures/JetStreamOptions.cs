using Squadron;

namespace Mocha.Transport.Nats.Tests.Fixtures;

/// <summary>
/// Squadron options for a NATS container with JetStream enabled.
/// </summary>
/// <remarks>
/// Squadron's default NATS options start the server without JetStream, so the <c>-js</c> flag has
/// to be added explicitly. Without it every stream and consumer call fails with
/// "jetstream not enabled".
/// <para>
/// The monitoring port is also passed explicitly. Adding a command replaces the image's default
/// (<c>--config nats-server.conf</c>), which is what would otherwise open port 8222, and Squadron
/// probes that port to decide the container is ready.
/// </para>
/// </remarks>
public sealed class JetStreamOptions : ContainerResourceOptions, IComposableResourceOption
{
    /// <summary>
    /// The lowest server version supporting everything the transport offers: per-message TTL landed
    /// in 2.11 and message schedules in 2.12. Pinned to the floor rather than to a moving tag, so
    /// the version-gated tests actually run and so a new dependency on a later server shows up as a
    /// failure here rather than in production.
    /// </summary>
    public const string Image = "nats:2.12-alpine";

    /// <inheritdoc />
    public Type ResourceType => typeof(NatsResource<JetStreamOptions>);

    /// <inheritdoc />
    public override void Configure(ContainerResourceBuilder builder)
    {
        builder
            .Name("nats-jetstream")
            .Image(Image)
            .AddCmd("-js", "-m", "8222")
            .AddVariable("nats-monitoring", VariableType.DynamicPort)
            .InternalPort(4222)
            .AddPortMapping(8222, "nats-monitoring")
            .PreferLocalImage();
    }
}
