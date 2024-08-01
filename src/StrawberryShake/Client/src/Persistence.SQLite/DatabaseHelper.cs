using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;

namespace StrawberryShake.Persistence.SQLite;

internal sealed class DatabaseHelper
{
    private const string _entitiesTable = @"
            CREATE TABLE IF NOT EXISTS strawberryShake_Entities(
                Id TEXT PRIMARY KEY,
                Value TEXT NOT NULL,
                Type TEXT NOT NULL)";

    private const string _operationsTable = @"
            CREATE TABLE IF NOT EXISTS strawberryShake_Operations(
                id TEXT PRIMARY KEY,
                Variables TEXT NULL,
                ResultType TEXT NOT NULL,
                DataInfo TEXT NOT NULL)";

    private readonly SqliteCommand _updateEntity = CreateUpdateEntityCommand();
    private readonly SqliteCommand _deleteEntity = CreateDeleteEntityCommand();
    private readonly SqliteCommand _loadEntities = CreateLoadEntitiesCommand();

    private readonly SqliteCommand _updateOperation = CreateUpdateOperationCommand();
    private readonly SqliteCommand _deleteOperation = CreateDeleteOperationCommand();
    private readonly SqliteCommand _loadOperations = CreateLoadOperationsCommand();

    public async Task CreateIfNotExistsAsync(SqliteConnection connection)
    {
        await CreateEntitiesTableAsync(connection).ConfigureAwait(false);
        await CreateOperationsTableAsync(connection).ConfigureAwait(false);
    }

    private static async Task CreateEntitiesTableAsync(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = _entitiesTable;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task CreateOperationsTableAsync(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = _operationsTable;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task<bool> SaveEntityAsync(
        SqliteConnection connection,
        EntityDto dto,
        CancellationToken cancellationToken = default)
    {
        _updateEntity.Connection = connection;

        _updateEntity.Parameters[0].Value = dto.Id;
        _updateEntity.Parameters[1].Value = dto.Value;
        _updateEntity.Parameters[2].Value = dto.Type;

        return await _updateEntity.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteEntityAsync(
        SqliteConnection connection,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        _deleteEntity.Connection = connection;

        _deleteEntity.Parameters[0].Value = entityId;

        return await _deleteEntity.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async IAsyncEnumerable<EntityDto> GetAllEntitiesAsync(
        SqliteConnection connection,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _loadEntities.Connection = connection;

        using var reader =
            await _loadEntities.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new EntityDto
            {
                Id = reader.GetString(0),
                Value = reader.GetString(1),
                Type = reader.GetString(2),
            };
        }
    }

    public async Task<bool> SaveOperationAsync(
        SqliteConnection connection,
        OperationDto dto,
        CancellationToken cancellationToken = default)
    {
        _updateOperation.Connection = connection;

        _updateOperation.Parameters[0].Value = dto.Id;
        _updateOperation.Parameters[1].Value = dto.Variables;
        _updateOperation.Parameters[2].Value = dto.ResultType;
        _updateOperation.Parameters[3].Value = dto.DataInfo;

        return await _updateOperation.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteOperationAsync(
        SqliteConnection connection,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        _deleteOperation.Connection = connection;

        _deleteOperation.Parameters[0].Value = operationId;

        return await _deleteOperation.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async IAsyncEnumerable<OperationDto> GetAllOperationsAsync(
        SqliteConnection connection,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _loadOperations.Connection = connection;

        using var reader =
            await _loadOperations.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return new OperationDto
            {
                Id = reader.GetString(0),
                Variables = reader.GetString(1),
                ResultType = reader.GetString(2),
                DataInfo = reader.GetString(3),
            };
        }
    }

    private static SqliteCommand CreateUpdateEntityCommand()
    {
        var command = new SqliteCommand(
            @"INSERT OR REPLACE INTO strawberryShake_Entities
                    (Id, Value, Type)
                VALUES
                    (@Id, @Value, @Type);");

        command.Parameters.Add("@Id", SqliteType.Text);
        command.Parameters.Add("@Value", SqliteType.Text);
        command.Parameters.Add("@Type", SqliteType.Text);

        return command;
    }

    private static SqliteCommand CreateDeleteEntityCommand()
    {
        var command = new SqliteCommand(
            "DELETE FROM strawberryShake_Entities WHERE Id = @Id");

        command.Parameters.Add("@Id", SqliteType.Text);

        return command;
    }

    private static SqliteCommand CreateLoadEntitiesCommand()
    {
        var command = new SqliteCommand(
            "SELECT Id, Value, Type FROM strawberryShake_Entities");
        return command;
    }

    private static SqliteCommand CreateUpdateOperationCommand()
    {
        var command = new SqliteCommand(
            @"INSERT OR REPLACE INTO strawberryShake_Operations
                    (Id, Variables, ResultType, DataInfo)
                VALUES
                    (@Id, @Variables, @ResultType, @DataInfo);");

        command.Parameters.Add("@Id", SqliteType.Text);
        command.Parameters.Add("@Variables", SqliteType.Text);
        command.Parameters.Add("@ResultType", SqliteType.Text);
        command.Parameters.Add("@DataInfo", SqliteType.Text);

        return command;
    }

    private static SqliteCommand CreateDeleteOperationCommand()
    {
        var command = new SqliteCommand(
            "DELETE FROM strawberryShake_Operations WHERE Id = @Id");

        command.Parameters.Add("@Id", SqliteType.Text);

        return command;
    }

    private static SqliteCommand CreateLoadOperationsCommand()
    {
        var command = new SqliteCommand(
            "SELECT Id, Variables, ResultType, DataInfo FROM strawberryShake_Operations");
        return command;
    }
}

internal class EntityDto
{
    public string Id { get; set; } = default!;

    public string Value { get; set; } = default!;

    public string Type { get; set; } = default!;
}

internal class OperationDto
{
    public string Id { get; set; } = default!;

    public string? Variables { get; set; }

    public string ResultType { get; set; } = default!;

    public string DataInfo { get; set; } = default!;
}
