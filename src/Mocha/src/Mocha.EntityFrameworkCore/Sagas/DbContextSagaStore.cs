using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Utils;

namespace Mocha.Sagas.EfCore;

/// <summary>
/// An EF Core-backed saga store scoped to a single <see cref="DbContext"/> lifetime,
/// implementing <see cref="ISagaStore"/> for saga state persistence.
/// </summary>
/// <remarks>
/// This store uses the provided <see cref="DbContext"/> for all database operations
/// including transaction management, state serialization via JSON documents, and
/// optimistic concurrency via version stamps. It is designed to be created per-scope
/// and disposed alongside the owning DbContext. Serialization is performed under a
/// lock to safely reuse pooled buffers.
/// </remarks>
/// <param name="context">The EF Core <see cref="DbContext"/> used for saga state persistence.</param>
internal sealed class DbContextSagaStore(DbContext context) : ISagaStore, IDisposable
{
    private readonly object _lock = new();
    private PooledArrayWriter? _arrayWriter;
    private List<byte[]>? _buffers;

    /// <summary>
    /// Creates a new <see cref="DbContextSagaStore"/> by resolving the specified DbContext type from the service provider.
    /// </summary>
    /// <param name="contextType">The <see cref="Type"/> of the DbContext to resolve.</param>
    /// <param name="services">The service provider used to resolve the DbContext.</param>
    /// <returns>A new <see cref="DbContextSagaStore"/> backed by the resolved DbContext.</returns>
    public static DbContextSagaStore Create(Type contextType, IServiceProvider services)
    {
        var dbContext = (DbContext)services.GetRequiredService(contextType);
        return new DbContextSagaStore(dbContext);
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
        if (context.Database.CurrentTransaction is not { })
        {
            var currentTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

            return new EfCoreSagaTransaction(currentTransaction);
        }

        return NoOpSagaTransaction.Instance;
    }

    /// <summary>
    /// Persists the saga state, inserting a new record or updating the existing one via EF Core change tracking.
    /// </summary>
    /// <typeparam name="T">The saga state type derived from <see cref="SagaStateBase"/>.</typeparam>
    /// <param name="saga">The saga definition providing name and serialization metadata.</param>
    /// <param name="state">The saga state instance to persist.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public async Task SaveAsync<T>(Saga saga, T state, CancellationToken cancellationToken) where T : SagaStateBase
    {
        var set = context.Set<SagaState>();
        var sagaState = await set.AsTracking()
            .Where(x => x.SagaName == saga.Name && x.Id == state.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var document = ToJsonDocument(saga, state);

        if (sagaState is null)
        {
            sagaState ??= new SagaState(
                state.Id,
                saga.Name,
                document,
                // TODO timeprovider
                DateTime.UtcNow,
                DateTimeOffset.UtcNow);
            sagaState.Version = NewVersion();
            set.Add(sagaState);
        }
        else
        {
            sagaState.State = document;
            sagaState.UpdatedAt = DateTime.UtcNow;
            sagaState.Version = NewVersion();
            set.Entry(sagaState).Property(x => x.State).IsModified = true;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes the persisted saga state for the given saga and instance identifier, if it exists.
    /// </summary>
    /// <param name="saga">The saga definition identifying which saga type to delete.</param>
    /// <param name="id">The unique identifier of the saga instance to remove.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    public async Task DeleteAsync(Saga saga, Guid id, CancellationToken cancellationToken)
    {
        var set = context.Set<SagaState>();

        var sagaState = await set.AsTracking()
            .FirstOrDefaultAsync(x => x.SagaName == saga.Name && x.Id == id, cancellationToken: cancellationToken);

        if (sagaState is not null)
        {
            set.Remove(sagaState);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Loads and deserializes the saga state for the given saga and instance identifier.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize the saga state into.</typeparam>
    /// <param name="saga">The saga definition providing name and deserialization metadata.</param>
    /// <param name="id">The unique identifier of the saga instance to load.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The deserialized saga state, or <c>default</c> if no state is found for the given identifier.</returns>
    public async Task<T?> LoadAsync<T>(Saga saga, Guid id, CancellationToken cancellationToken)
    {
        // as the state is scoped we load the whole saga state into memory for the concurrency check
        var sageState = await context
            .Set<SagaState>()
            .AsTracking()
            .Where(x => x.SagaName == saga.Name && x.Id == id)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (sageState?.State is not { } document)
        {
            return default;
        }

        try
        {
            return FromJsonDocument<T>(saga, document);
        }
        finally
        {
            document.Dispose();
        }
    }

    private JsonDocument ToJsonDocument(Saga saga, SagaStateBase state)
    {
        lock (_lock)
        {
            _arrayWriter ??= new PooledArrayWriter();
            _buffers ??= [];

            _arrayWriter.Reset();
            saga.StateSerializer.Serialize(state, _arrayWriter);

            var writtenMemory = _arrayWriter.WrittenMemory;

            var rentedArray = BufferPools.Rent(writtenMemory.Length);
            _buffers.Add(rentedArray);

            writtenMemory.CopyTo(rentedArray);

            var document = JsonDocument.Parse(rentedArray.AsMemory()[..writtenMemory.Length]);

            return document;
        }
    }

    private T? FromJsonDocument<T>(Saga saga, JsonDocument document)
    {
        lock (_lock)
        {
            _arrayWriter ??= new PooledArrayWriter();
            _arrayWriter.Reset();

            using var writer = new Utf8JsonWriter(_arrayWriter);
            document.WriteTo(writer);
            writer.Flush();

            var writtenMemory = _arrayWriter.WrittenMemory;

            return saga.StateSerializer.Deserialize<T>(writtenMemory) ?? default;
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
    /// Releases the pooled array writer and returns all rented byte buffers to the pool.
    /// </summary>
    public void Dispose()
    {
        _arrayWriter?.Dispose();
        if (_buffers is not null)
        {
            foreach (var buffer in _buffers)
            {
                BufferPools.Return(buffer);
            }
        }
    }
}
