using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LiteDB;
using Newtonsoft.Json;
using StrawberryShake.Internal;
using StrawberryShake.Json;
using static StrawberryShake.Json.JsonSerializationHelper;

namespace StrawberryShake.Persistence.SQLite
{
    public class LiteDBPersistence : IDisposable
    {
        public const string Entities = nameof(Entities);
        public const string Operations = nameof(Operations);

        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            Formatting = Formatting.None,
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        };

        private readonly JsonOperationRequestSerializer _requestSerializer = new();
        private readonly CancellationTokenSource _cts = new();

        private readonly Channel<object> _queue =
            Channel.CreateUnbounded<object>();

        private readonly IStoreAccessor _storeAccessor;
        private readonly LiteDatabase _database;
        private readonly bool _disposeDatabase;

        private IDisposable? _entityStoreSubscription;
        private IDisposable? _operationStoreSubscription;
        private bool _disposed;

        public LiteDBPersistence(IStoreAccessor storeAccessor, string connectionString)
        {
            _storeAccessor = storeAccessor;
            _database = new LiteDatabase(connectionString);
            _disposeDatabase = true;
        }

        public LiteDBPersistence(IStoreAccessor storeAccessor, LiteDatabase database)
        {
            _storeAccessor = storeAccessor;
            _database = database;
            _disposeDatabase = false;
        }

        public void BeginInitialize()
        {
            Task.Run(Initialize);
        }

        public void Initialize()
        {
            ReadEntities();
            ReadOperations();
            BeginWrite();
        }

        private void ReadEntities()
        {
            _storeAccessor.EntityStore.Update(session =>
            {
                var collection = _database.GetCollection<EntityDto>(Entities);
                foreach (var entityDto in collection.FindAll())
                {
                    using var json = JsonDocument.Parse(entityDto.Id);
                    EntityId entityId =
                        _storeAccessor.EntityIdSerializer.Parse(json.RootElement);
                    Type type = Type.GetType(entityDto.TypeName)!;
                    object entity = JsonConvert.DeserializeObject(
                        entityDto.Entity,
                        type,
                        _serializerSettings);
                    session.SetEntity(entityId, entity);
                }
            });
        }

        private void ReadOperations()
        {
            var collection = _database.GetCollection<OperationDto>(Operations);

            foreach (var operationDto in collection.FindAll())
            {
                var resultType = Type.GetType(operationDto.ResultTypeName)!;
                var variables = operationDto.Variables is not null
                    ? ReadDictionary(operationDto.Variables)
                    : null;
                var dataInfo = JsonConvert.DeserializeObject<IOperationResultDataInfo>(
                    operationDto.DataInfo,
                    _serializerSettings);

                var requestFactory = _storeAccessor.GetOperationRequestFactory(resultType);
                var dataFactory = _storeAccessor.GetOperationResultDataFactory(resultType);

                OperationRequest request = requestFactory.Create(variables);
                IOperationResult result = OperationResult.Create(
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
                    onNext: update =>
                    {
                        _queue.Writer.TryWrite(update);
                    },
                    onCompleted: () => _cts.Cancel());

            Task.Run(async () => await WriteAsync(_cts.Token));
        }

        private async Task WriteAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested ||
                    !_queue.Reader.Completion.IsCompleted)
                {
                    var update = await _queue.Reader.ReadAsync(cancellationToken);

                    if (update is EntityUpdate entityUpdate)
                    {
                        var collection = _database.GetCollection<EntityDto>(Entities);

                        foreach (EntityId entityId in entityUpdate.UpdatedEntityIds)
                        {
                            WriteEntity(entityId, entityUpdate.Snapshot, collection);
                        }
                    }
                    else if (update is OperationUpdate operationUpdate)
                    {
                        var collection = _database.GetCollection<OperationDto>(Operations);

                        if (operationUpdate.Kind == OperationUpdateKind.Updated)
                        {
                            foreach (StoredOperationVersion operationVersion in
                                operationUpdate.OperationVersions)
                            {
                                WriteOperation(operationVersion, collection);
                            }
                        }
                        else if (operationUpdate.Kind == OperationUpdateKind.Removed)
                        {
                            foreach (StoredOperationVersion operationVersion in
                                operationUpdate.OperationVersions)
                            {
                                collection.Delete(operationVersion.Request.GetHash());
                            }
                        }
                    }
                }
            }
            catch (ChannelClosedException) { }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        private void WriteEntity(
            EntityId entityId,
            IEntityStoreSnapshot snapshot,
            ILiteCollection<EntityDto> collection)
        {
            try
            {
                string serializedId = _storeAccessor.EntityIdSerializer.Format(entityId);

                if (snapshot.TryGetEntity(entityId, out object? entity))
                {
                    collection.Upsert(
                        serializedId,
                        new EntityDto
                        {
                            Id = serializedId,
                            Entity = JsonConvert.SerializeObject(entity,
                                _serializerSettings),
                            TypeName = entity.GetType().FullName!,
                            Version = snapshot.Version
                        });
                }
                else
                {
                    collection.Delete(serializedId);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void WriteOperation(
            StoredOperationVersion operationVersion,
            ILiteCollection<OperationDto> collection)
        {
            if (operationVersion.Result is not null &&
                operationVersion.Result.Errors.Count == 0 &&
                operationVersion.Result.DataInfo is not null)
            {
                using var writer = new ArrayWriter();
                _requestSerializer.Serialize(operationVersion.Request, writer);
                Type dataType = operationVersion.Result.DataType;


                var operationDto = new OperationDto
                {
                    Id = operationVersion.Request.GetHash(),
                    Variables = WriteValue(operationVersion.Request.Variables),
                    DataInfo = JsonConvert.SerializeObject(
                        operationVersion.Result.DataInfo,
                        _serializerSettings),
                    ResultTypeName =  $"{dataType.FullName}, {dataType.Assembly.GetName().Name}"
                };

                collection.Upsert(operationDto.Id, operationDto);
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

                if (_disposeDatabase)
                {
                    _database.Dispose();
                }

                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }
    }

    public class OperationDto
    {
        public string Id { get; set; } = default!;

        public string? Variables { get; set; }

        public string ResultTypeName { get; set; } = default!;

        public string DataInfo { get; set; } = default!;
    }

    public class EntityDto
    {
        public string Id { get; set; }

        public string Entity { get; set; }

        public string TypeName { get; set; }

        public ulong Version { get; set; }
    }
}
