using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// Default implementation of <see cref="IEntityFrameworkCoreBuilder"/> that carries the service
/// collection, host builder, DbContext type, and logical name used during Entity Framework Core
/// feature registration.
/// </summary>
internal class EntityFrameworkCoreBuilder : IEntityFrameworkCoreBuilder
{
    /// <inheritdoc />
    public required IServiceCollection Services { get; init; }

    /// <inheritdoc />
    public required IMessageBusHostBuilder HostBuilder { get; init; }

    /// <inheritdoc />
    public required Type ContextType { get; init; }

    /// <inheritdoc />
    public required string Name { get; init; }
}
