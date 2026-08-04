using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mocha.EntityFrameworkCore;
using Mocha.EntityFrameworkCore.Postgres;
using Mocha.Sagas.EfCore;
using EfCoreSagaState = Mocha.Sagas.EfCore.SagaState;

namespace Mocha.Sagas;

/// <summary>
/// Provides extension methods on <see cref="IEntityFrameworkCoreBuilder"/> for registering
/// Postgres-backed saga state persistence using raw SQL with optimistic concurrency.
/// </summary>
public static class SagaServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Postgres saga store infrastructure: table info discovery from the EF Core model,
    /// pre-built SQL queries, and a scoped <see cref="ISagaStore"/> backed by direct Npgsql commands.
    /// </summary>
    /// <param name="builder">The Entity Framework Core builder to configure.</param>
    /// <returns>The same <paramref name="builder"/> instance for chaining.</returns>
    public static IEntityFrameworkCoreBuilder AddPostgresSagas(this IEntityFrameworkCoreBuilder builder)
    {
        var contextType = builder.ContextType;

        // Configure PostgresTableInfo for SagaState
        builder
            .Services.AddOptions<PostgresTableInfo>(builder.Name)
            .Configure<IServiceProvider>(
                (options, sp) =>
                {
                    using var scope = sp.CreateScope();
                    var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
                    var model = dbContext.Model;

                    ConfigureSagaStateTableInfo(options.SagaState, model);
                });

        // Configure PostgresSagaStoreOptions with pre-built queries
        builder
            .Services.AddOptions<PostgresSagaStoreOptions>(builder.Name)
            .Configure<IOptionsMonitor<PostgresTableInfo>>(
                (options, tableInfoMonitor) =>
                {
                    var tableInfo = tableInfoMonitor.Get(builder.Name);
                    options.Queries = PostgresSagaStoreQueries.From(tableInfo.SagaState);
                });

        // Register PostgresSagaStore
        builder.Services.TryAddScoped<ISagaStore>(sp => PostgresSagaStore.Create(contextType, builder.Name, sp));

        return builder;
    }

    private static void ConfigureSagaStateTableInfo(SagaStateTableInfo sagaState, IModel model)
    {
        // Use the EfCore SagaState entity type
        var sagaEntity = model.FindEntityType(typeof(EfCoreSagaState));
        if (sagaEntity is null)
        {
            return;
        }

        var tableName = sagaEntity.GetTableName();
        var schema = sagaEntity.GetSchema();

        if (tableName is not null)
        {
            sagaState.Table = tableName;
        }

        if (schema is not null)
        {
            sagaState.Schema = schema;
        }

        var storeObject = StoreObjectIdentifier.Create(sagaEntity, StoreObjectType.Table);
        if (storeObject is null)
        {
            return;
        }

        var idProperty = sagaEntity.FindProperty(nameof(EfCoreSagaState.Id));
        if (idProperty is not null)
        {
            var columnName = idProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                sagaState.Id = columnName;
            }
        }

        var versionProperty = sagaEntity.FindProperty(nameof(EfCoreSagaState.Version));
        if (versionProperty is not null)
        {
            var columnName = versionProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                sagaState.Version = columnName;
            }
        }

        var sagaNameProperty = sagaEntity.FindProperty(nameof(EfCoreSagaState.SagaName));
        if (sagaNameProperty is not null)
        {
            var columnName = sagaNameProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                sagaState.SagaName = columnName;
            }
        }

        var stateProperty = sagaEntity.FindProperty(nameof(EfCoreSagaState.State));
        if (stateProperty is not null)
        {
            var columnName = stateProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                sagaState.State = columnName;
            }
        }

        var createdAtProperty = sagaEntity.FindProperty(nameof(EfCoreSagaState.CreatedAt));
        if (createdAtProperty is not null)
        {
            var columnName = createdAtProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                sagaState.CreatedAt = columnName;
            }
        }

        var updatedAtProperty = sagaEntity.FindProperty(nameof(EfCoreSagaState.UpdatedAt));
        if (updatedAtProperty is not null)
        {
            var columnName = updatedAtProperty.GetColumnName(storeObject.Value);
            if (columnName is not null)
            {
                sagaState.UpdatedAt = columnName;
            }
        }
    }
}
