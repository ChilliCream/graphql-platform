using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.SqlServer;

namespace Mocha.Scheduling;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// the SQL Server scheduling infrastructure including the dispatcher, worker, and message persistence.
/// </summary>
public static class SchedulingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the full SQL Server scheduling pipeline: table info discovery from the EF Core model,
    /// the <see cref="SqlServerScheduledMessageDispatcher"/>, a hosted background worker, and a scoped
    /// <see cref="IScheduledMessageStore"/> backed by direct SQL Server inserts.
    /// </summary>
    /// <remarks>
    /// This method also calls <see cref="SchedulingEntityFrameworkCorePersistenceBuilderExtensions.UseSchedulingCore"/>
    /// to register the EF Core interceptors that signal the scheduler on save and commit.
    /// </remarks>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder UseSqlServerScheduling(this IEntityFrameworkCoreBuilder builder)
    {
        var contextType = builder.ContextType;

        builder
            .Services.AddOptions<SqlServerTableInfo>(builder.Name)
            .Configure<IServiceProvider>((options, sp) =>
            {
                using var scope = sp.CreateScope();
                var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
                var model = dbContext.Model;

                ConfigureScheduledMessageTableInfo(options.ScheduledMessage, model);
            });

        builder
            .Services.AddOptions<SqlServerScheduledMessageOptions>(builder.Name)
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
                options.Queries = SqlServerScheduledMessageQueries.From(tableInfo.ScheduledMessage);
            });

        builder.Services.AddSingleton(sp =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<SqlServerScheduledMessageOptions>>();
            var options = optionsMonitor.Get(builder.Name);
            return new SqlServerScheduledMessageDispatcher(
                sp.GetRequiredService<ILogger<SqlServerScheduledMessageDispatcher>>(),
                sp,
                sp.GetRequiredService<IMessagingRuntime>(),
                sp.GetRequiredService<IMessagingPools>(),
                sp.GetRequiredService<ISchedulerSignal>(),
                options.Queries);
        });

        builder.Services.AddSingleton(sp =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<SqlServerScheduledMessageOptions>>();
            var options = optionsMonitor.Get(builder.Name);
            return new SqlServerScheduledMessageWorker(options, sp.GetRequiredService<SqlServerScheduledMessageDispatcher>());
        });

        builder.Services.AddHostedService(sp => sp.GetRequiredService<SqlServerScheduledMessageWorker>());

        builder.Services.TryAddScoped<IScheduledMessageStore>(sp =>
            SqlServerScheduledMessageStore.Create(contextType, builder.Name, sp)
        );

        builder.UseSchedulingCore();

        return builder;
    }

    private static void ConfigureScheduledMessageTableInfo(ScheduledMessageTableInfo scheduledMessage, IModel model)
    {
        var entity = model.FindEntityType(typeof(ScheduledMessage));
        if (entity is null)
        {
            return;
        }

        var tableName = entity.GetTableName();
        var schema = entity.GetSchema();

        if (tableName is not null)
        {
            scheduledMessage.Table = tableName;
        }

        if (schema is not null)
        {
            scheduledMessage.Schema = schema;
        }

        var storeObject = StoreObjectIdentifier.Create(entity, StoreObjectType.Table);
        if (storeObject is null)
        {
            return;
        }

        var idProperty = entity.FindProperty(nameof(ScheduledMessage.Id));
        if (idProperty is not null)
        {
            var columnName = idProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                scheduledMessage.Id = columnName;
            }
        }

        var envelopeProperty = entity.FindProperty(nameof(ScheduledMessage.Envelope));
        if (envelopeProperty is not null)
        {
            var columnName = envelopeProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                scheduledMessage.Envelope = columnName;
            }
        }

        var scheduledTimeProperty = entity.FindProperty(nameof(ScheduledMessage.ScheduledTime));
        if (scheduledTimeProperty is not null)
        {
            var columnName = scheduledTimeProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                scheduledMessage.ScheduledTime = columnName;
            }
        }

        var timesSentProperty = entity.FindProperty(nameof(ScheduledMessage.TimesSent));
        if (timesSentProperty is not null)
        {
            var columnName = timesSentProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                scheduledMessage.TimesSent = columnName;
            }
        }

        var createdAtProperty = entity.FindProperty(nameof(ScheduledMessage.CreatedAt));
        if (createdAtProperty is not null)
        {
            var columnName = createdAtProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                scheduledMessage.CreatedAt = columnName;
            }
        }

        var maxAttemptsProperty = entity.FindProperty(nameof(ScheduledMessage.MaxAttempts));
        if (maxAttemptsProperty is not null)
        {
            var columnName = maxAttemptsProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                scheduledMessage.MaxAttempts = columnName;
            }
        }

        var lastErrorProperty = entity.FindProperty(nameof(ScheduledMessage.LastError));
        if (lastErrorProperty is not null)
        {
            var columnName = lastErrorProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                scheduledMessage.LastError = columnName;
            }
        }
    }
}
