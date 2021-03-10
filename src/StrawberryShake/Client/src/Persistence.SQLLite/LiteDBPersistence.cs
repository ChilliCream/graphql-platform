using System;
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
            Formatting = Formatting.None, TypeNameHandling = TypeNameHandling.All
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
                    Type type = Type.GetType(entityDto.TypeName)!;
                    object entity = JsonConvert.DeserializeObject(
                        entityDto.Entity,
                        type,
                        _serializerSettings);
                    EntityId entityId = JsonConvert.DeserializeObject<EntityId>(
                        entityDto.Id,
                        _serializerSettings);
                    session.SetEntity(entityId, entity);
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
            using var writer = new ArrayWriter();
            _requestSerializer.Serialize(operationVersion.Request, writer);

            var operationDto = new OperationDto
            {
                Id = operationVersion.Request.GetHash(),
                Request = Convert.ToBase64String(writer.GetInternalBuffer(), 0, writer.Length),
                DataInfo = JsonConvert.SerializeObject(operationVersion.Result!.DataInfo),
                DataType = operationVersion.Result.DataType.FullName!
            };

            collection.Upsert(operationDto.Id, operationDto);
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
        public string Id { get; set; }

        public string Request { get; set; }

        public string DataInfo { get; set; }

        public string DataType { get; set; }
    }

    public class EntityDto
    {
        public string Id { get; set; }

        public string Entity { get; set; }

        public string TypeName { get; set; }

        public ulong Version { get; set; }
    }
}
