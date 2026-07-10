using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.Utils;
using Npgsql;
using NpgsqlTypes;

namespace Mocha.Sagas.EfCore;

internal sealed class PostgresSagaStore(DbContext context, PostgresSagaStoreQueries queries, TimeProvider timeProvider)
    : ISagaStore
    , IDisposable
{
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<SagaStateKey, Guid> _versions = new();
    private PooledArrayWriter? _arrayWriter;

    /// <summary>
    /// Creates a new <see cref="PostgresSagaStore"/> by resolving the DbContext, saga store options,
    /// and time provider from the service provider.
    /// </summary>
    /// <param name="contextType">The <see cref="Type"/> of the DbContext to resolve.</param>
    /// <param name="optionsName">The named options key used to retrieve <see cref="PostgresSagaStoreOptions"/>.</param>
    /// <param name="services">The scoped service provider used to resolve dependencies.</param>
    /// <returns>A new <see cref="PostgresSagaStore"/> configured with pre-built SQL queries.</returns>
    public static PostgresSagaStore Create(Type contextType, string optionsName, IServiceProvider services)
    {
        var dbContext = (DbContext)services.GetRequiredService(contextType);
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<PostgresSagaStoreOptions>>();
        var options = optionsMonitor.Get(optionsName);
        var timeProvider = services.GetService<TimeProvider>() ?? TimeProvider.System;

        return new PostgresSagaStore(dbContext, options.Queries, timeProvider);
    }

    /// <summary>
    /// Starts a new saga transaction if no database transaction is already active on the DbContext;
    /// otherwise returns a no-op transaction to avoid nesting.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>
    /// An <see cref="EfCoreSagaTransaction"/> wrapping a new database transaction, or
    /// <see cref="NoOpSagaTransaction.Instance"/> when a transaction is already in progress.
    /// </returns>
    public async Task<ISagaTransaction> StartTransactionAsync(CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is not null)
        {
            return NoOpSagaTransaction.Instance;
        }

        var currentTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

        return new EfCoreSagaTransaction(currentTransaction);
    }

    /// <summary>
    /// Persists the saga state using raw SQL against Postgres. States loaded by this store are
    /// updated with optimistic concurrency control using the version captured at load time. States
    /// that were not loaded by this store are inserted and conflict if a row already exists.
    /// </summary>
    /// <typeparam name="T">The saga state type derived from <see cref="SagaStateBase"/>.</typeparam>
    /// <param name="saga">The saga definition providing name and serialization metadata.</param>
    /// <param name="state">The saga state instance to persist.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <exception cref="DbUpdateConcurrencyException">
    /// Thrown when the saga state was modified by another process between load and save.
    /// </exception>
    public async Task SaveAsync<T>(Saga saga, T state, CancellationToken cancellationToken) where T : SagaStateBase
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction() as NpgsqlTransaction;

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var jsonData = SerializeState(saga, state);
        var newVersion = NewVersion();
        var now = timeProvider.GetUtcNow();
        var key = new SagaStateKey(saga.Name, state.Id);

        if (!_versions.TryGetValue(key, out var version))
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = queries.InsertState;
            cmd.Transaction = transaction;
            cmd.Parameters.AddWithValue("@id", state.Id);
            cmd.Parameters.AddWithValue("@sagaName", saga.Name);
            cmd.Parameters.Add(new NpgsqlParameter("@state", NpgsqlDbType.Json) { Value = jsonData });
            cmd.Parameters.AddWithValue("@createdAt", now);
            cmd.Parameters.AddWithValue("@updatedAt", now);
            cmd.Parameters.AddWithValue("@version", newVersion);
            await cmd.PrepareAsync(cancellationToken);

            try
            {
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new DbUpdateConcurrencyException(
                    "The saga state was concurrently created or already exists.",
                    ex);
            }

            _versions[key] = newVersion;
        }
        else
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = queries.UpdateState;
            cmd.Transaction = transaction;
            cmd.Parameters.Add(new NpgsqlParameter("@state", NpgsqlDbType.Json) { Value = jsonData });
            cmd.Parameters.AddWithValue("@updatedAt", now);
            cmd.Parameters.AddWithValue("@newVersion", newVersion);
            cmd.Parameters.AddWithValue("@id", state.Id);
            cmd.Parameters.AddWithValue("@sagaName", saga.Name);
            cmd.Parameters.AddWithValue("@oldVersion", version);
            await cmd.PrepareAsync(cancellationToken);

            var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected == 0)
            {
                throw new DbUpdateConcurrencyException("The saga state was modified by another process.");
            }

            _versions[key] = newVersion;
        }
    }

    /// <summary>
    /// Deletes the persisted saga state for the given saga and instance identifier using raw SQL.
    /// </summary>
    /// <param name="saga">The saga definition identifying which saga type to delete.</param>
    /// <param name="id">The unique identifier of the saga instance to remove.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public async Task DeleteAsync(Saga saga, Guid id, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = queries.DeleteState;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@sagaName", saga.Name);
        await cmd.PrepareAsync(cancellationToken);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _versions.TryRemove(new SagaStateKey(saga.Name, id), out _);
    }

    /// <summary>
    /// Loads and deserializes the saga state for the given saga and instance identifier using raw SQL.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize the saga state into.</typeparam>
    /// <param name="saga">The saga definition providing name and deserialization metadata.</param>
    /// <param name="id">The unique identifier of the saga instance to load.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The deserialized saga state, or <c>default</c> if no state is found for the given identifier.</returns>
    public async Task<T?> LoadAsync<T>(Saga saga, Guid id, CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction() as NpgsqlTransaction;

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = queries.SelectState;
        cmd.Transaction = transaction;
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@sagaName", saga.Name);
        await cmd.PrepareAsync(cancellationToken);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return default;
        }

        var stateJson = reader.GetFieldValue<ReadOnlyMemory<byte>>(0);
        var version = reader.GetFieldValue<Guid>(1);
        var state = saga.StateSerializer.Deserialize<T>(stateJson);

        if (state is SagaStateBase)
        {
            _versions[new SagaStateKey(saga.Name, id)] = version;
        }

        return state;
    }

    private byte[] SerializeState(Saga saga, SagaStateBase state)
    {
        lock (_lock)
        {
            _arrayWriter ??= new PooledArrayWriter();
            _arrayWriter.Reset();

            saga.StateSerializer.Serialize(state, _arrayWriter);

            return _arrayWriter.WrittenMemory.ToArray();
        }
    }

    private static Guid NewVersion()
    {
#if NET9_0_OR_GREATER
        return Guid.CreateVersion7();
#else
        return Guid.NewGuid();
#endif
    }

    /// <summary>
    /// Releases the pooled array writer used for saga state serialization.
    /// </summary>
    public void Dispose()
    {
        _arrayWriter?.Dispose();
    }

    private readonly record struct SagaStateKey(string SagaName, Guid Id);
}
