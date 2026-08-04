using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.Postgres;

namespace Mocha.Inbox;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// the Postgres inbox infrastructure including the cleanup worker and message deduplication.
/// </summary>
public static class InboxServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full Postgres inbox pipeline: table info discovery from the EF Core model,
    /// pre-built SQL queries, a hosted background cleanup worker, and a scoped
    /// <see cref="IMessageInbox"/> backed by direct Npgsql commands.
    /// </summary>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <param name="configure">An optional action to configure <see cref="InboxOptions"/>.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder UsePostgresInbox(
        this IEntityFrameworkCoreBuilder builder,
        Action<InboxOptions>? configure = null)
    {
        var contextType = builder.ContextType;

        builder.HostBuilder.UseInboxCore();

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }
        else
        {
            builder.Services.Configure<InboxOptions>(static _ => { });
        }

        // Configure PostgresTableInfo for Inbox
        builder
            .Services.AddOptions<PostgresTableInfo>(builder.Name)
            .Configure<IServiceProvider>((options, sp) =>
            {
                using var scope = sp.CreateScope();
                var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
                var model = dbContext.Model;

                ConfigureInboxTableInfo(options.Inbox, model);
            });

        // Configure PostgresMessageInboxOptions with pre-built queries
        builder
            .Services.AddOptions<PostgresMessageInboxOptions>(builder.Name)
            .Configure<IServiceProvider, IOptionsMonitor<PostgresTableInfo>>((options, sp, tableInfoMonitor) =>
            {
                using var scope = sp.CreateScope();
                var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
                options.ConnectionString =
                    dbContext.Database.GetConnectionString() ??
                    throw new InvalidOperationException(
                        $"Could not read the connection string from {contextType.Name}");
                var tableInfo = tableInfoMonitor.Get(builder.Name);
                options.Queries = PostgresMessageInboxQueries.From(tableInfo.Inbox);
            });

        builder.Services.AddSingleton(sp =>
            new MessageBusInboxWorker(
                sp.GetRequiredService<IOptions<InboxOptions>>(),
                sp,
                sp.GetService<TimeProvider>() ?? TimeProvider.System,
                sp.GetRequiredService<ILogger<InboxCleanupProcessor>>(),
                sp.GetRequiredService<ILogger<MessageBusInboxWorker>>()));

        builder.Services.AddHostedService(sp => sp.GetRequiredService<MessageBusInboxWorker>());

        builder.Services.TryAddScoped<IMessageInbox>(sp =>
            PostgresMessageInbox.Create(contextType, builder.Name, sp));

        return builder;
    }

    private static void ConfigureInboxTableInfo(InboxTableInfo inbox, IModel model)
    {
        var inboxEntity = model.FindEntityType(typeof(InboxMessage));
        if (inboxEntity is null)
        {
            return;
        }

        var tableName = inboxEntity.GetTableName();
        var schema = inboxEntity.GetSchema();

        if (tableName is not null)
        {
            inbox.Table = tableName;
        }

        if (schema is not null)
        {
            inbox.Schema = schema;
        }

        var storeObject = StoreObjectIdentifier.Create(inboxEntity, StoreObjectType.Table);
        if (storeObject is null)
        {
            return;
        }

        var messageIdProperty = inboxEntity.FindProperty(nameof(InboxMessage.MessageId));
        if (messageIdProperty is not null)
        {
            var columnName = messageIdProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                inbox.MessageId = columnName;
            }
        }

        var consumerTypeProperty = inboxEntity.FindProperty(nameof(InboxMessage.ConsumerType));
        if (consumerTypeProperty is not null)
        {
            var columnName = consumerTypeProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                inbox.ConsumerType = columnName;
            }
        }

        var messageTypeProperty = inboxEntity.FindProperty(nameof(InboxMessage.MessageType));
        if (messageTypeProperty is not null)
        {
            var columnName = messageTypeProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                inbox.MessageType = columnName;
            }
        }

        var processedAtProperty = inboxEntity.FindProperty(nameof(InboxMessage.ProcessedAt));
        if (processedAtProperty is not null)
        {
            var columnName = processedAtProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                inbox.ProcessedAt = columnName;
            }
        }
    }
}
