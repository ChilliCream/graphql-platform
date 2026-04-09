using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.SqlServer;

namespace Mocha.Outbox;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// the SQL Server outbox infrastructure including the outbox processor, worker, and message persistence.
/// </summary>
public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full SQL Server outbox pipeline: table info discovery from the EF Core model,
    /// the <see cref="SqlServerOutboxProcessor"/>, a hosted background worker, and a scoped
    /// <see cref="IMessageOutbox"/> backed by direct SQL Server inserts.
    /// </summary>
    /// <remarks>
    /// This method also calls <see cref="OutboxEntityFrameworkCorePersistenceBuilderExtensions.UseOutboxCore"/>
    /// to register the EF Core interceptors that signal the processor on save and commit.
    /// </remarks>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder UseSqlServerOutbox(this IEntityFrameworkCoreBuilder builder)
    {
        var contextType = builder.ContextType;

        builder
            .Services.AddOptions<SqlServerTableInfo>(builder.Name)
            .Configure<IServiceProvider>((options, sp) =>
            {
                using var scope = sp.CreateScope();
                var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
                var model = dbContext.Model;

                ConfigureOutboxTableInfo(options.Outbox, model);
            });

        builder
            .Services.AddOptions<SqlServerMessageOutboxOptions>(builder.Name)
            .Configure<IServiceProvider, IOptionsMonitor<SqlServerTableInfo>>((options, sqlServerOptions,
                tableInfoMonitor) =>
            {
                using var scope = sqlServerOptions.CreateScope();
                var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
                options.ConnectionString =
                    dbContext.Database.GetConnectionString() ??
                    throw new InvalidOperationException(
                        $"Could not read the connection string from {contextType.Name}");
                var tableInfo = tableInfoMonitor.Get(builder.Name);
                options.Queries = SqlServerMessageOutboxQueries.From(tableInfo.Outbox);
            });

        builder.Services.AddSingleton(sp =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<SqlServerMessageOutboxOptions>>();
            var options = optionsMonitor.Get(builder.Name);
            return new SqlServerOutboxProcessor(
                sp.GetRequiredService<ILogger<SqlServerOutboxProcessor>>(),
                sp,
                sp.GetRequiredService<IMessagingRuntime>(),
                sp.GetRequiredService<IMessagingPools>(),
                sp.GetRequiredService<IOutboxSignal>(),
                options.Queries);
        });

        builder.Services.AddSingleton(sp =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<SqlServerMessageOutboxOptions>>();
            var options = optionsMonitor.Get(builder.Name);
            return new SqlServerMessageBusOutboxWorker(options, sp.GetRequiredService<SqlServerOutboxProcessor>());
        });

        builder.Services.AddHostedService(sp => sp.GetRequiredService<SqlServerMessageBusOutboxWorker>());

        builder.Services.TryAddScoped<IMessageOutbox>(sp =>
            SqlServerMessageOutbox.Create(contextType, builder.Name, sp)
        );

        builder.UseOutboxCore();

        return builder;
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
}
