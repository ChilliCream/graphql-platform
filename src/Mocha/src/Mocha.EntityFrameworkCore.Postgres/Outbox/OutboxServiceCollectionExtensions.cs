using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.Postgres;
using Npgsql;

namespace Mocha.Outbox;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// the Postgres outbox infrastructure including the outbox processor, worker, and message persistence.
/// </summary>
public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full Postgres outbox pipeline: table info discovery from the EF Core model,
    /// the <see cref="PostgresOutboxProcessor"/>, a hosted background worker, and a scoped
    /// <see cref="IMessageOutbox"/> backed by direct Npgsql inserts.
    /// </summary>
    /// <remarks>
    /// This method also calls <see cref="OutboxEntityFrameworkCorePersistenceBuilderExtensions.UseOutboxCore"/>
    /// to register the EF Core interceptors that signal the processor on save and commit.
    /// </remarks>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder UsePostgresOutbox(this IEntityFrameworkCoreBuilder builder)
        => builder.UsePostgresOutbox(static _ => { });

    /// <summary>
    /// Registers the full Postgres outbox pipeline with the specified <see cref="PostgresOutboxOptions"/>.
    /// </summary>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <param name="configure">A delegate that configures the <see cref="PostgresOutboxOptions"/>.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder UsePostgresOutbox(
        this IEntityFrameworkCoreBuilder builder,
        Action<PostgresOutboxOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.UseOutboxCore();
        builder.Services.AddOptions<PostgresOutboxOptions>(builder.Name).Configure(configure);

        if (builder.Services.Any(descriptor =>
            descriptor.ServiceType == typeof(PostgresOutboxRegistration)
            && ((PostgresOutboxRegistration)descriptor.ImplementationInstance!).ContextType == builder.ContextType))
        {
            return builder;
        }

        builder.Services.AddSingleton(new PostgresOutboxRegistration(builder.ContextType, builder.Name));

        var contextType = builder.ContextType;

        builder
            .Services.AddOptions<PostgresTableInfo>(builder.Name)
            .Configure<IServiceProvider>((options, sp) =>
            {
                using var scope = sp.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredKeyedService<DbContextAccessor>(contextType).Context;
                var model = dbContext.Model;

                ConfigureOutboxTableInfo(options.Outbox, model);
            });

        builder
            .Services.AddOptions<PostgresMessageOutboxOptions>(builder.Name)
            .Configure<IOptionsMonitor<PostgresTableInfo>, IServiceProvider>((options, tableInfoMonitor, provider) =>
            {
                options.CreateConnection = services =>
                {
                    var dbContext = services.GetRequiredKeyedService<DbContextAccessor>(contextType).Context;
                    return (NpgsqlConnection)((ICloneable)dbContext.Database.GetDbConnection()).Clone();
                };

                var tableInfo = tableInfoMonitor.Get(builder.Name);
                options.Queries = PostgresMessageOutboxQueries.From(tableInfo.Outbox);

                using var scope = provider.CreateScope();
                var configuredContext = scope.ServiceProvider
                    .GetRequiredKeyedService<DbContextAccessor>(contextType).Context;
                var configuredConnection = (NpgsqlConnection)configuredContext.Database.GetDbConnection();
                var configuredSettings = new NpgsqlConnectionStringBuilder(configuredConnection.ConnectionString);
                var host = configuredSettings.Host;
                var port = configuredSettings.Port;
                var database = configuredSettings.Database;
                var searchPath = configuredSettings.SearchPath;
                var username = configuredSettings.Username;
                var connectionOptions = configuredSettings.Options;
                var configuredEntity = configuredContext.Model.FindEntityType(typeof(OutboxMessage));
                var tableName = configuredEntity?.GetTableName();
                var schema = configuredEntity?.GetSchema();
                var columns = new (string Property, string Column)[]
                {
                    (nameof(OutboxMessage.Id), tableInfo.Outbox.Id),
                    (nameof(OutboxMessage.Envelope), tableInfo.Outbox.Envelope),
                    (nameof(OutboxMessage.TimesSent), tableInfo.Outbox.TimesSent),
                    (nameof(OutboxMessage.CreatedAt), tableInfo.Outbox.CreatedAt)
                };

                options.MatchesDestination = context =>
                {
                    if (context.Database.GetDbConnection() is not NpgsqlConnection connection)
                    {
                        return false;
                    }

                    var settings = new NpgsqlConnectionStringBuilder(connection.ConnectionString);
                    if (settings.Host != host
                        || settings.Port != port
                        || settings.Database != database
                        || settings.SearchPath != searchPath
                        || settings.Username != username
                        || settings.Options != connectionOptions)
                    {
                        return false;
                    }

                    var entity = context.Model.FindEntityType(typeof(OutboxMessage));
                    if (entity is null || entity.GetTableName() != tableName || entity.GetSchema() != schema
                        || StoreObjectIdentifier.Create(entity, StoreObjectType.Table) is not { } table)
                    {
                        return false;
                    }

                    foreach (var (property, column) in columns)
                    {
                        if (entity.FindProperty(property)?.GetColumnName(table) != column)
                        {
                            return false;
                        }
                    }

                    return true;
                };
            });

        builder.Services.AddKeyedSingleton<PostgresOutboxProcessor>(contextType, (sp, _) =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>();
            var options = optionsMonitor.Get(builder.Name);
            return new PostgresOutboxProcessor(
                sp.GetRequiredService<ILogger<PostgresOutboxProcessor>>(),
                sp,
                sp.GetRequiredService<IMessagingRuntime>(),
                sp.GetRequiredService<IMessagingPools>(),
                sp.GetRequiredService<IOutboxSignal>(),
                options);
        });

        builder.Services.AddSingleton(sp => sp.GetRequiredKeyedService<PostgresOutboxProcessor>(contextType));
        builder.Services.AddKeyedSingleton<PostgresMessageBusOutboxWorker>(contextType, (sp, _) =>
            new PostgresMessageBusOutboxWorker(sp.GetRequiredKeyedService<PostgresOutboxProcessor>(contextType)));
        builder.Services.AddSingleton(sp => sp.GetRequiredKeyedService<PostgresMessageBusOutboxWorker>(contextType));

        builder.Services.AddSingleton<IHostedService>(sp =>
            sp.GetRequiredKeyedService<PostgresMessageBusOutboxWorker>(contextType));

        builder.Services.TryAddScoped<IMessageOutbox>(CreateDefaultOutbox);

        builder.ConfigureEntityFrameworkServices((applicationServices, services) =>
        {
            services.AddScoped<PostgresOutboxTracker>();
            services.AddScoped<IInterceptor>(sp => sp.GetRequiredService<PostgresOutboxTracker>());
            services.AddScoped<IResettableService>(sp => sp.GetRequiredService<PostgresOutboxTracker>());
            services.AddScoped<IDbContextOutboxProvider>(sp => new PostgresDbContextOutboxProvider(
                sp.GetRequiredService<PostgresOutboxTracker>(),
                () => applicationServices.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>()
                    .Get(builder.Name)));
        });

        return builder;
    }

    private static IMessageOutbox CreateDefaultOutbox(IServiceProvider services)
    {
        var registrations = services.GetServices<PostgresOutboxRegistration>().ToArray();
        var defaultContext = services.GetService<DefaultOutboxDbContextRegistration>();
        PostgresOutboxRegistration registration;

        if (defaultContext is not null)
        {
            registration = registrations.FirstOrDefault(r => r.ContextType == defaultContext.ContextType)
                ?? throw Mocha.EntityFrameworkCore.ThrowHelper.DefaultOutboxDbContextNotRegistered(
                    defaultContext.ContextType);
        }
        else if (registrations.Length == 1)
        {
            registration = registrations[0];
        }
        else
        {
            throw Mocha.EntityFrameworkCore.ThrowHelper.AmbiguousOutboxDbContext();
        }

        return PostgresMessageOutbox.Create(registration.ContextType, registration.Name, services);
    }

    private static void ConfigureOutboxTableInfo(OutboxTableInfo outbox, IModel model)
    {
        var outboxEntity = model.FindEntityType(typeof(OutboxMessage));
        if (outboxEntity is null)
        {
            return;
        }

        var tableName = outboxEntity.GetTableName();
        var schema = outboxEntity.GetSchema();

        if (tableName is not null)
        {
            outbox.Table = tableName;
        }

        if (schema is not null)
        {
            outbox.Schema = schema;
        }

        var storeObject = StoreObjectIdentifier.Create(outboxEntity, StoreObjectType.Table);
        if (storeObject is null)
        {
            return;
        }

        var idProperty = outboxEntity.FindProperty(nameof(OutboxMessage.Id));
        if (idProperty is not null)
        {
            var columnName = idProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                outbox.Id = columnName;
            }
        }

        var envelopeProperty = outboxEntity.FindProperty(nameof(OutboxMessage.Envelope));
        if (envelopeProperty is not null)
        {
            var columnName = envelopeProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                outbox.Envelope = columnName;
            }
        }

        var timesSentProperty = outboxEntity.FindProperty(nameof(OutboxMessage.TimesSent));
        if (timesSentProperty is not null)
        {
            var columnName = timesSentProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                outbox.TimesSent = columnName;
            }
        }

        var createdAtProperty = outboxEntity.FindProperty(nameof(OutboxMessage.CreatedAt));
        if (createdAtProperty is not null)
        {
            var columnName = createdAtProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                outbox.CreatedAt = columnName;
            }
        }
    }

    private sealed record PostgresOutboxRegistration(Type ContextType, string Name);
}
