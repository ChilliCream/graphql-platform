using System.Data;
using System.Transactions;
using Npgsql;

namespace Mocha.Transport.Postgres;

public static class PostgresTransportSchema
{
    /// <summary>
    /// Applies pending transport migrations in a transaction on an open, caller-owned connection.
    /// The connection must have no active or ambient transaction and remains open after the operation.
    /// </summary>
    /// <param name="connection">The open connection to the database to migrate.</param>
    /// <param name="schemaOptions">The schema and table naming used by the transport.</param>
    /// <param name="cancellationToken">A token to cancel migration, including waiting for its lock.</param>
    public static async Task MigrateAsync(
        NpgsqlConnection connection,
        IReadOnlyPostgresSchemaOptions schemaOptions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(schemaOptions);
        cancellationToken.ThrowIfCancellationRequested();

        if (connection.State != ConnectionState.Open)
        {
            throw ThrowHelper.MigrationConnectionMustBeOpen();
        }

        if (Transaction.Current is not null)
        {
            throw ThrowHelper.MigrationTransactionNotSupported(null);
        }

        var sql = PostgresSchemaSql.GenerateMigrationsSql(schemaOptions);
        NpgsqlTransaction transaction;
        try
        {
            transaction = await connection.BeginTransactionAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw ThrowHelper.MigrationTransactionNotSupported(exception);
        }

        await using (transaction)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Returns an idempotent forward migration script with transaction boundaries and LF line endings.
    /// Generation is synchronous and does not access a database or file.
    /// </summary>
    /// <param name="schemaOptions">The schema and table naming used by the transport.</param>
    public static string GenerateMigrationsSql(IReadOnlyPostgresSchemaOptions schemaOptions)
    {
        ArgumentNullException.ThrowIfNull(schemaOptions);

        return "BEGIN;\n" + PostgresSchemaSql.GenerateMigrationsSql(schemaOptions) + "COMMIT;\n";
    }
}
