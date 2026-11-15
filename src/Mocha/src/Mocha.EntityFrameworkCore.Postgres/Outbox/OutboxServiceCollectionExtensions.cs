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

public static class OutboxServiceCollectionExtensions
{
    public static IEntityFrameworkCoreBuilder UsePostgresOutbox(this IEntityFrameworkCoreBuilder builder)
        => builder.UsePostgresOutbox(static _ => { });

    public static IEntityFrameworkCoreBuilder UsePostgresOutbox(
        this IEntityFrameworkCoreBuilder builder,
        Action<PostgresOutboxOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        builder.UseOutboxCore();
        builder.Services.AddOptions<PostgresOutboxOptions>(builder.Name)
            .Configure(configure)
            .Validate(options => options.QueryDelay > TimeSpan.Zero, "The outbox query delay must be greater than zero.");

        if (builder.Services.Any(descriptor =>
            descriptor.ServiceType == typeof(PostgresOutboxRegistration)
            && descriptor.ImplementationInstance is PostgresOutboxRegistration registration
            && registration.ContextType == builder.ContextType))
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
                var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
                var model = dbContext.Model;

                ConfigureOutboxTableInfo(options.Outbox, model);
            });

        builder
            .Services.AddOptions<PostgresMessageOutboxOptions>(builder.Name)
            .Configure<IOptionsMonitor<PostgresTableInfo>>((options, tableInfoMonitor) =>
            {
                options.CreateConnection = services =>
                {
                    var dbContext = (DbContext)services.GetRequiredService(contextType);
                    return (NpgsqlConnection)((ICloneable)dbContext.Database.GetDbConnection()).Clone();
                };

                var tableInfo = tableInfoMonitor.Get(builder.Name);
                options.Queries = PostgresMessageOutboxQueries.From(tableInfo.Outbox);
            });

        builder.Services.AddKeyedSingleton(contextType, (sp, _) =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<PostgresMessageOutboxOptions>>();
            var options = optionsMonitor.Get(builder.Name);
            var outboxOptions = sp.GetRequiredService<IOptionsMonitor<PostgresOutboxOptions>>().Get(builder.Name);
            return new PostgresOutboxProcessor(
                sp.GetRequiredService<ILogger<PostgresOutboxProcessor>>(),
                sp,
                sp.GetRequiredService<IMessagingRuntime>(),
                sp.GetRequiredService<IMessagingPools>(),
                sp.GetRequiredService<IOutboxSignal>(),
                options,
                outboxOptions.QueryDelay);
        });

        builder.Services.AddSingleton<IHostedService>(sp =>
            new PostgresMessageBusOutboxWorker(sp.GetRequiredKeyedService<PostgresOutboxProcessor>(contextType)));

        builder.Services.TryAddScoped<IMessageOutbox>(CreateDefaultOutbox);

        builder.ConfigureEntityFrameworkServices((applicationServices, services) =>
        {
            services.AddScoped(_ => new PostgresDbContextOutbox(applicationServices.GetTimeProvider()));
            services.AddScoped<IInterceptor>(sp => sp.GetRequiredService<PostgresDbContextOutbox>());
            services.AddScoped<IResettableService>(sp => sp.GetRequiredService<PostgresDbContextOutbox>());
            services.AddScoped<IDbContextOutbox>(sp => sp.GetRequiredService<PostgresDbContextOutbox>());
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
