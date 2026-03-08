using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mocha.Middlewares;
using Npgsql;

namespace Mocha.Inbox;

/// <summary>
/// Implements <see cref="IMessageInbox"/> for Postgres by recording processed message identifiers
/// in the inbox table using raw SQL through the DbContext Npgsql connection.
/// </summary>
/// <remarks>
/// When a database transaction is active on the DbContext, the inbox operations participate in
/// that transaction to ensure atomicity with message processing.
/// A <see cref="SemaphoreSlim"/> serializes access to the shared connection to prevent concurrent
/// command execution on the same Npgsql connection.
/// </remarks>
internal sealed class PostgresMessageInbox : IMessageInbox, IDisposable
{
    private readonly DbContext _dbContext;
    private readonly NpgsqlConnection _connection;
    private readonly PostgresMessageInboxQueries _queries;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Creates a new <see cref="PostgresMessageInbox"/> using the provided DbContext connection
    /// and pre-built SQL queries.
    /// </summary>
    /// <param name="dbContext">The DbContext whose underlying Npgsql connection is used for inbox operations.</param>
    /// <param name="connection">The Npgsql connection to use for inbox queries.</param>
    /// <param name="queries">The pre-built SQL queries for inbox operations.</param>
    /// <param name="timeProvider">The time provider used for computing cleanup cutoff timestamps.</param>
    internal PostgresMessageInbox(
        DbContext dbContext,
        NpgsqlConnection connection,
        PostgresMessageInboxQueries queries,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _connection = connection;
        _queries = queries;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async ValueTask<bool> ExistsAsync(
        string messageId,
        string consumerType,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectionOpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(_queries.Exists, _connection);

            var activeTransaction = GetActiveTransaction();

            if (activeTransaction is not null)
            {
                command.Transaction = activeTransaction;
            }

            command.Parameters.AddWithValue("message_id", messageId);
            command.Parameters.AddWithValue("consumer_type", consumerType);

            await command.PrepareAsync(cancellationToken);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null && (bool)result;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryClaimAsync(
        MessageEnvelope envelope,
        string consumerType,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectionOpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(_queries.TryClaim, _connection);

            var activeTransaction = GetActiveTransaction();

            if (activeTransaction is not null)
            {
                command.Transaction = activeTransaction;
            }

            command.Parameters.AddWithValue("message_id", envelope.MessageId ?? string.Empty);
            command.Parameters.AddWithValue("consumer_type", consumerType);
            command.Parameters.AddWithValue("message_type",
                (object?)envelope.MessageType ?? DBNull.Value);

            await command.PrepareAsync(cancellationToken);
            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            return rowsAffected > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask RecordAsync(
        MessageEnvelope envelope,
        string consumerType,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectionOpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(_queries.Insert, _connection);

            var activeTransaction = GetActiveTransaction();

            if (activeTransaction is not null)
            {
                command.Transaction = activeTransaction;
            }

            command.Parameters.AddWithValue("message_id", envelope.MessageId ?? string.Empty);
            command.Parameters.AddWithValue("consumer_type", consumerType);
            command.Parameters.AddWithValue("message_type",
                (object?)envelope.MessageType ?? DBNull.Value);

            await command.PrepareAsync(cancellationToken);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask<int> CleanupAsync(
        TimeSpan maxAge,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectionOpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(_queries.Cleanup, _connection);

            var activeTransaction = GetActiveTransaction();

            if (activeTransaction is not null)
            {
                command.Transaction = activeTransaction;
            }

            command.Parameters.AddWithValue("cutoff", _timeProvider.GetUtcNow().UtcDateTime - maxAge);

            await command.PrepareAsync(cancellationToken);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Retrieves the active <see cref="NpgsqlTransaction"/> from the DbContext, if one exists.
    /// </summary>
    /// <returns>The active transaction, or <c>null</c> if no transaction is in progress.</returns>
    private NpgsqlTransaction? GetActiveTransaction()
        => _dbContext
            .Database
            .CurrentTransaction
            ?.GetDbTransaction() as NpgsqlTransaction;

    /// <summary>
    /// Ensures the underlying Npgsql connection is open before executing a command.
    /// If the connection is in a <see cref="System.Data.ConnectionState.Broken"/> state,
    /// it is closed first and then re-opened to recover from transient failures.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    private async ValueTask EnsureConnectionOpenAsync(CancellationToken cancellationToken)
    {
        if (_connection.State == System.Data.ConnectionState.Broken)
        {
            await _connection.CloseAsync();
        }

        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Releases the semaphore used for connection serialization.
    /// </summary>
    public void Dispose()
    {
        _semaphore.Dispose();
    }

    /// <summary>
    /// Creates a new <see cref="PostgresMessageInbox"/> by resolving the DbContext and named inbox
    /// options from the scoped service provider.
    /// </summary>
    /// <param name="contextType">The <see cref="Type"/> of the DbContext to resolve.</param>
    /// <param name="optionsName">The named options key used to retrieve <see cref="PostgresMessageInboxOptions"/>.</param>
    /// <param name="services">The scoped service provider used to resolve dependencies.</param>
    /// <returns>A new <see cref="PostgresMessageInbox"/> configured for the specified DbContext.</returns>
    public static PostgresMessageInbox Create(Type contextType, string optionsName, IServiceProvider services)
    {
        var dbContext = (DbContext)services.GetRequiredService(contextType);
        var connection = dbContext.Database.GetDbConnection() as NpgsqlConnection
            ?? throw new InvalidOperationException("Not an Npgsql connection.");

        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<PostgresMessageInboxOptions>>();
        var options = optionsMonitor.Get(optionsName);
        var timeProvider = services.GetService<TimeProvider>() ?? TimeProvider.System;

        return new PostgresMessageInbox(dbContext, connection, options.Queries, timeProvider);
    }
}
