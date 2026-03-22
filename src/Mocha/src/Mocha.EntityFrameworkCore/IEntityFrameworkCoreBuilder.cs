using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Defines the contract for configuring Entity Framework Core messaging persistence features
/// (outbox, sagas, resilience) for a specific DbContext within the message bus host.
/// </summary>
/// <remarks>
/// This builder is scoped to messaging persistence concerns and is not a general-purpose
/// EF Core configuration interface. It is obtained via
/// <c>AddEntityFramework&lt;TContext&gt;()</c> on the message bus host builder.
/// </remarks>
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
