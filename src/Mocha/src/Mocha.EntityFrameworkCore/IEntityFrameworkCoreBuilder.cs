using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

// TODO this interface is probably too generic (naming)
/// <summary>
/// Defines the contract for configuring Entity Framework Core persistence features
/// (outbox, sagas, resilience) for a specific DbContext within the message bus host.
/// </summary>
public interface IEntityFrameworkCoreBuilder
{
    /// <summary>
    /// Gets the application service collection for registering dependencies.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the parent message bus host builder used to configure messaging middleware and features.
    /// </summary>
    IMessageBusHostBuilder HostBuilder { get; }

    /// <summary>
    /// Gets the <see cref="Type"/> of the DbContext associated with this builder.
    /// </summary>
    Type ContextType { get; }

    /// <summary>
    /// Gets the logical name used to identify this builder's configuration, typically the DbContext full type name.
    /// </summary>
    string Name { get; }
}
