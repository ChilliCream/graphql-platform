using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using LiteDB;
using Newtonsoft.Json;
using StrawberryShake.Internal;
using StrawberryShake.Json;

namespace StrawberryShake.Persistence.SQLite
{
    public class LiteDBPersistence : IDisposable
    {
        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            Formatting = Formatting.None,
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
        };

        private readonly JsonOperationRequestSerializer _requestSerializer = new();
        private readonly CancellationTokenSource _cts = new();

        private readonly Channel<EntityUpdate> _entityQueue =
            Channel.CreateUnbounded<EntityUpdate>();

        private readonly Channel<OperationUpdate> _operationQueue =
            Channel.CreateUnbounded<OperationUpdate>();

        private readonly IStoreAccessor _storeAccessor;
        private readonly LiteDatabase _database;
        private IDisposable? _entityStoreSubscription;
        private IDisposable? _operationStoreSubscription;
        private bool _disposed;

        public LiteDBPersistence(IStoreAccessor storeAccessor, LiteDatabase database)
        {
            _storeAccessor = storeAccessor;
            _database = database;
        }

        public void Begin()
        {
            Task.Run(() =>
            {
                ReadEntities();
                BeginWrite();
            });
        }

        private void ReadEntities()
        {
            var collection = _database.GetCollection<EntityDto>("entities");

            _storeAccessor.EntityStore.Update(session =>
            {
                foreach (var entityDto in collection.FindAll())
                {
                    EntityId entityId = entityDto.EntityId;
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
            var collection = _database.GetCollection<OperationDto>("operations");

            _storeAccessor.EntityStore.Update(session =>
            {
                foreach (var entityDto in collection.FindAll())
                {
                    /*
                    var resultType = Type.GetType(entityDto.ResultTypeName)!;
                    var variables = entityDto.Variables is not null
                        ? JsonConvert.DeserializeObject<Dictionary<string, object?>>(
                            entityDto.Variables,
                            _serializerSettings)
                        : null;
                    var dataInfo = JsonConvert.DeserializeObject<IOperationResultDataInfo>(
                        entityDto.DataInfo,
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
                    */
                }
            });
        }

        private void BeginWrite()
        {
            _entityStoreSubscription = _storeAccessor.EntityStore
                .Watch()
                .Subscribe(
                    onNext: update => _entityQueue.Writer.TryWrite(update),
                    onCompleted: () => _cts.Cancel());

            _operationStoreSubscription = _storeAccessor.OperationStore
                .Watch()
                .Subscribe(
                    onNext: update => _operationQueue.Writer.TryWrite(update),
                    onCompleted: () => _cts.Cancel());

            BeginWriteEntities();
            BeginWriteOperations();
        }

        private void BeginWriteEntities() =>
            Task.Run(async () => await WriteEntitiesAsync(_cts.Token));

        private void BeginWriteOperations() =>
            Task.Run(async () => await WriteOperationsAsync(_cts.Token));

        private async Task WriteEntitiesAsync(CancellationToken cancellationToken)
        {
            var collection = _database.GetCollection<EntityDto>("entities");

#if NETSTANDARD2_0 || NETSTANDARD2_1
            while (!cancellationToken.IsCancellationRequested ||
                !_entityQueue.Reader.Completion.IsCompleted)
            {
                var update = await _entityQueue.Reader.ReadAsync(cancellationToken);

                foreach (EntityId entityId in update.UpdatedEntityIds)
                {
                    WriteEntity(entityId, update.Snapshot, collection);
                }
            }
#else
            await foreach (EntityUpdate update in
                _entityQueue.Reader.ReadAllAsync(cancellationToken))
            {
                foreach (EntityId entityId in update.UpdatedEntityIds)
                {
                    WriteEntity(entityId, update.Snapshot, collection);
                }
            }
#endif
        }

        private void WriteEntity(
            EntityId entityId,
            IEntityStoreSnapshot snapshot,
            ILiteCollection<EntityDto> collection)
        {
            string serializedId = JsonConvert.SerializeObject(entityId, _serializerSettings);

            if (snapshot.TryGetEntity(entityId, out object? entity))
            {
                string typeName = entity.GetType().FullName!;

                collection.Upsert(
                    serializedId,
                    new EntityDto
                    {
                        Id = serializedId,
                        EntityId = entityId,
                        Entity = JsonConvert.SerializeObject(entity, _serializerSettings),
                        TypeName = typeName,
                        Version = snapshot.Version
                    });
            }
            else
            {
                collection.Delete(serializedId);
            }
        }

        private async Task WriteOperationsAsync(CancellationToken cancellationToken)
        {
            var collection = _database.GetCollection<OperationDto>("operations");

#if NETSTANDARD2_0 || NETSTANDARD2_1
            while (!cancellationToken.IsCancellationRequested ||
                   !_operationQueue.Reader.Completion.IsCompleted)
            {
                var update = await _operationQueue.Reader.ReadAsync(cancellationToken);

                foreach (StoredOperationVersion operationVersion in update.OperationVersions)
                {
                    WriteOperation(operationVersion, collection);
                }
            }
#else
            await foreach (OperationUpdate update in
                _operationQueue.Reader.ReadAllAsync(cancellationToken))
            {
                if (update.Kind == OperationUpdateKind.Updated)
                {
                    foreach (StoredOperationVersion operationVersion in update.OperationVersions)
                    {
                        WriteOperation(operationVersion, collection);
                    }
                }
                else if (update.Kind == OperationUpdateKind.Removed)
                {
                    foreach (StoredOperationVersion operationVersion in update.OperationVersions)
                    {
                        DeleteOperation(operationVersion, collection);
                    }
                }
            }
#endif
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

                var operationDto = new OperationDto
                {
                    Id = operationVersion.Request.GetHash(),
                    Variables = operationVersion.Request.Variables.ToDictionary(
                        t => t.Key,
                        t => t.Value),
                    DataInfo = operationVersion.Result.DataInfo,
                    ResultTypeName = operationVersion.Result.DataType.FullName!
                };

                collection.Upsert(operationDto.Id, operationDto);
            }
        }

        private void DeleteOperation(
            StoredOperationVersion operationVersion,
            ILiteCollection<OperationDto> collection)
        {
            collection.Delete(operationVersion.Request.GetHash());
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cts.Cancel();
                _database.Dispose();
                _entityStoreSubscription?.Dispose();
                _operationStoreSubscription?.Dispose();
                _cts.Dispose();
                _disposed = true;
            }
        }
    }

    public class OperationDto
    {
        public string Id { get; set; } = default!;

        public Dictionary<string, object?>? Variables { get; set; }

        public string ResultTypeName { get; set; } = default!;

        public IOperationResultDataInfo DataInfo { get; set; } = default!;
    }

    public class EntityDto
    {
        public string Id { get; set; }

        public EntityId EntityId { get; set; }

        public string Entity { get; set; }

        public string TypeName { get; set; }

        public ulong Version { get; set; }
    }
}
