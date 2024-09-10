using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using StrawberryShake.Internal;
using StrawberryShake.Json;
using static StrawberryShake.Json.JsonSerializationHelper;

namespace StrawberryShake.Persistence.SQLite;

public class SQLitePersistence : IDisposable
{
    private readonly JsonSerializerSettings _serializerSettings = new()
    {
        Formatting = Formatting.None,
        TypeNameHandling = TypeNameHandling.All,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
    };

    private readonly JsonOperationRequestSerializer _requestSerializer = new();
    private readonly CancellationTokenSource _cts = new();

    private readonly Channel<object> _queue =
        Channel.CreateUnbounded<object>();

    private readonly IStoreAccessor _storeAccessor;
    private readonly string _connectionString;
    private IDisposable? _entityStoreSubscription;
    private IDisposable? _operationStoreSubscription;
    private bool _disposed;

    public SQLitePersistence(IStoreAccessor storeAccessor, string connectionString)
    {
        _storeAccessor = storeAccessor;
        _connectionString = connectionString;
    }

    public void BeginInitialize()
    {
        Task.Run(InitializeAsync);
    }

    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        var database = new DatabaseHelper();

        await connection.OpenAsync().ConfigureAwait(false);
        await database.CreateIfNotExistsAsync(connection).ConfigureAwait(false);
        await ReadEntitiesAsync(connection, database).ConfigureAwait(false);
        await ReadOperationsAsync(connection, database).ConfigureAwait(false);

        BeginWrite();
    }

    private async Task ReadEntitiesAsync(SqliteConnection connection, DatabaseHelper database)
    {
        var entities = new List<(EntityId, object)>();

        await foreach (var dto in database.GetAllEntitiesAsync(connection)
            .ConfigureAwait(false))
        {
            using var json = JsonDocument.Parse(dto.Id);
            var entityId = _storeAccessor.EntityIdSerializer.Parse(json.RootElement);
            var type = Type.GetType(dto.Type)!;
            var entity = JsonConvert.DeserializeObject(dto.Value, type, _serializerSettings)!;
            entities.Add((entityId, entity));
        }

        _storeAccessor.EntityStore.Update(session =>
        {
            foreach ((var id, var value) in entities)
            {
                session.SetEntity(id, value);
            }
        });
    }

    private async Task ReadOperationsAsync(SqliteConnection connection, DatabaseHelper database)
    {
        await foreach (var dto in database.GetAllOperationsAsync(connection)
            .ConfigureAwait(false))
        {
            var resultType = Type.GetType(dto.ResultType)!;
            var variables =
                dto.Variables is not null
                    ? ReadDictionary(dto.Variables)
                    : null;
            var dataInfo =
                JsonConvert.DeserializeObject<IOperationResultDataInfo>(
                    dto.DataInfo,
                    _serializerSettings)!;

            var requestFactory =
                _storeAccessor.GetOperationRequestFactory(resultType);
            var dataFactory =
                _storeAccessor.GetOperationResultDataFactory(resultType);

            var request = requestFactory.Create(variables);

            var result = OperationResult.Create(
                dataFactory.Create(dataInfo),
                resultType,
                dataInfo,
                dataFactory,
                null);

            _storeAccessor.OperationStore.Set(request, result);
        }
    }

    private void BeginWrite()
    {
        _entityStoreSubscription = _storeAccessor.EntityStore
            .Watch()
            .Subscribe(
                onNext: update => _queue.Writer.TryWrite(update),
                onCompleted: () => _cts.Cancel());

        _operationStoreSubscription = _storeAccessor.OperationStore
            .Watch()
            .Subscribe(
                onNext: update => _queue.Writer.TryWrite(update),
                onCompleted: () => _cts.Cancel());

        Task.Run(async () => await WriteAsync(_cts.Token).ConfigureAwait(false));
    }

    private async Task WriteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var database = new DatabaseHelper();

            while (!cancellationToken.IsCancellationRequested ||
                !_queue.Reader.Completion.IsCompleted)
            {
                var update = await _queue.Reader.ReadAsync(cancellationToken);
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                if (update is EntityUpdate entityUpdate)
                {
                    foreach (var entityId in entityUpdate.UpdatedEntityIds)
                    {
                        await WriteEntityAsync(
                            entityId,
                            entityUpdate.Snapshot,
                            connection,
                            database,
                            cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                else if (update is OperationUpdate operationUpdate)
                {
                    if (operationUpdate.Kind == OperationUpdateKind.Updated)
                    {
                        foreach (var operationVersion in
                            operationUpdate.OperationVersions)
                        {
                            await WriteOperationAsync(
                                operationVersion,
                                connection,
                                database,
                                cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                    else if (operationUpdate.Kind == OperationUpdateKind.Removed)
                    {
                        foreach (var operationVersion in
                            operationUpdate.OperationVersions)
                        {
                            await database.DeleteOperationAsync(
                                connection,
                                operationVersion.Request.GetHash(), cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                }
            }
        }
        catch (ChannelClosedException) { }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private async Task WriteEntityAsync(
        EntityId entityId,
        IEntityStoreSnapshot snapshot,
        SqliteConnection connection,
        DatabaseHelper database,
        CancellationToken cancellationToken)
    {
        var serializedId = _storeAccessor.EntityIdSerializer.Format(entityId);

        if (snapshot.TryGetEntity(entityId, out object? entity))
        {
            var dto = new EntityDto
            {
                Id = serializedId,
                Value = JsonConvert.SerializeObject(entity, _serializerSettings),
                Type = entity.GetType().FullName!,
            };

            await database.SaveEntityAsync(
                connection,
                dto,
                cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await database.DeleteEntityAsync(
                connection,
                serializedId,
                cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task WriteOperationAsync(
        StoredOperationVersion operationVersion,
        SqliteConnection connection,
        DatabaseHelper database,
        CancellationToken cancellationToken)
    {
        if (operationVersion.Result is not null &&
            operationVersion.Result.Errors.Count == 0 &&
            operationVersion.Result.DataInfo is not null)
        {
            using var writer = new ArrayWriter();
            _requestSerializer.Serialize(operationVersion.Request, writer);
            var dataType = operationVersion.Result.DataType;

            var dto = new OperationDto
            {
                Id = operationVersion.Request.GetHash(),
                Variables = WriteValue(operationVersion.Request.Variables),
                DataInfo = JsonConvert.SerializeObject(
                    operationVersion.Result.DataInfo,
                    _serializerSettings),
                ResultType = $"{dataType.FullName}, {dataType.Assembly.GetName().Name}",
            };

            await database.SaveOperationAsync(
                connection,
                dto,
                cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _queue.Writer.TryComplete();
            _cts.Cancel();

            _entityStoreSubscription?.Dispose();
            _operationStoreSubscription?.Dispose();
            _cts.Dispose();

            GC.SuppressFinalize(this);
            _disposed = true;
        }
    }
}
