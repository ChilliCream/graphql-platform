using System.Collections.Immutable;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Provides read access to the fully resolved messaging configuration including transports,
/// endpoints, consumers, and conventions.
/// </summary>
public interface IMessagingConfigurationContext : IFeatureProvider
{
    /// <summary>
    /// Gets the service provider used for resolving configuration-time dependencies.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets the naming conventions used to derive endpoint, queue, and exchange names.
    /// </summary>
    IBusNamingConventions Naming { get; }

    /// <summary>
    /// Gets the registry of all known message types and their serialization metadata.
    /// </summary>
    IMessageTypeRegistry Messages { get; }

    /// <summary>
    /// Gets the message router responsible for resolving outbound routes for message types.
    /// </summary>
    IMessageRouter Router { get; }

    /// <summary>
    /// Gets the endpoint router used to resolve dispatch and receive endpoints by address.
    /// </summary>
    IEndpointRouter Endpoints { get; }

    /// <summary>
    /// Gets the host information describing the current application instance.
    /// </summary>
    IHostInfo Host { get; }

    /// <summary>
    /// Gets the convention registry containing all registered configuration, topology, and endpoint
    /// conventions.
    /// </summary>
    IConventionRegistry Conventions { get; }

    /// <summary>
    /// Gets the immutable set of all consumers registered in this bus configuration.
    /// </summary>
    ImmutableHashSet<Consumer> Consumers { get; }

    /// <summary>
    /// Gets the immutable array of all transports configured for this bus.
    /// </summary>
    ImmutableArray<MessagingTransport> Transports { get; }
}
