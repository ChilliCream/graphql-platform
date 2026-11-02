using System.Collections.Immutable;
using Mocha.Middlewares;

namespace Mocha;

internal class MessagingSetupContext : IMessagingSetupContext
{
    public required IServiceProvider Services { get; init; }

    public required ImmutableHashSet<Consumer> Consumers { get; init; }

    public required ImmutableArray<MessagingTransport> Transports { get; init; }

    public required IBusNamingConventions Naming { get; init; }

    public required IHostInfo Host { get; init; }

    public required IFeatureCollection Features { get; init; }

    public required IMessageRouter Router { get; init; }

    public required IEndpointRouter Endpoints { get; init; }

    public required IMessageTypeRegistry Messages { get; init; }

    public required IConventionRegistry Conventions { get; init; }

    public MessagingTransport? Transport { get; set; }
}
