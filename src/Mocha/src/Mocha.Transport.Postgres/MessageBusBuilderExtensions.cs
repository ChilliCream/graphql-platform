using Microsoft.Extensions.DependencyInjection;
using Mocha.Scheduling;
using Mocha.Transport.Postgres.Scheduling;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Extension methods for registering the PostgreSQL messaging transport on an <see cref="IMessageBusHostBuilder"/>.
/// </summary>
public static class PostgresMessageBusBuilderExtensions
{
    /// <summary>
    /// Adds a PostgreSQL messaging transport to the message bus and configures it with the supplied delegate.
    /// </summary>
    /// <remarks>
    /// Default conventions (queue naming, topology discovery, dispatch topology) are automatically
    /// registered before the caller's configuration delegate runs.
    /// </remarks>
    /// <param name="busBuilder">The host builder to add the transport to.</param>
    /// <param name="configure">A delegate to configure endpoints, topology, middleware, and conventions.</param>
    /// <returns>The same <paramref name="busBuilder"/> for method chaining.</returns>
    public static IMessageBusHostBuilder AddPostgres(
        this IMessageBusHostBuilder busBuilder,
        Action<IPostgresMessagingTransportDescriptor> configure)
    {
        var transport = new PostgresMessagingTransport(x => configure(x.AddDefaults()));

        busBuilder.ConfigureMessageBus(b => b.AddTransport(transport));

        busBuilder.Services.AddScoped(_ => new PostgresTransportScheduledMessageStore(transport));

        busBuilder.Services.AddSingleton(
            new ScheduledMessageStoreRegistration(
                TransportType: typeof(PostgresMessagingTransport),
                TokenPrefix: "postgres-transport:",
                StoreType: typeof(PostgresTransportScheduledMessageStore)));

        return busBuilder;
    }

    /// <summary>
    /// Adds a PostgreSQL messaging transport to the message bus with the specified connection string.
    /// </summary>
    /// <param name="busBuilder">The host builder to add the transport to.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The same <paramref name="busBuilder"/> for method chaining.</returns>
    public static IMessageBusHostBuilder AddPostgres(
        this IMessageBusHostBuilder busBuilder,
        string connectionString)
    {
        return busBuilder.AddPostgres(x => x.ConnectionString(connectionString));
    }
}
