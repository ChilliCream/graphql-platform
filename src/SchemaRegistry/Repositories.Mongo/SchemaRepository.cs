using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MarshmallowPie.Repositories.Mongo
{
    public class SchemaRepository
        : ISchemaRepository
    {
        private readonly IMongoCollection<Schema> _schemas;
        private readonly IMongoCollection<SchemaVersion> _schemaVersions;

        public SchemaRepository(
            IMongoCollection<Schema> schemas,
            IMongoCollection<SchemaVersion> schemaVersions)
        {
            _schemas = schemas;
            _schemaVersions = schemaVersions;

            _schemas.Indexes.CreateOne(
                new CreateIndexModel<Schema>(
                    Builders<Schema>.IndexKeys.Ascending(x => x.Name),
                    new CreateIndexOptions { Unique = true }));
        }

        public IQueryable<Schema> GetSchemas() => _schemas.AsQueryable();

        public Task<Schema> GetSchemaAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return _schemas.AsQueryable()
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyDictionary<Guid, Schema>> GetSchemasAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            var list = new List<Guid>(ids);

            List<Schema> result = await _schemas.AsQueryable()
                .Where(t => list.Contains(t.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Id);
        }


        public Task AddSchemaAsync(
            Schema schema,
            CancellationToken cancellationToken = default)
        {
            return _schemas.InsertOneAsync(
                schema,
                options: null,
                cancellationToken);
        }

        public Task UpdateSchemaAsync(
            Schema schema,
            CancellationToken cancellationToken = default)
        {
            return _schemas.ReplaceOneAsync(
                Builders<Schema>.Filter.Eq(t => t.Id, schema.Id),
                schema,
                options: default(ReplaceOptions),
                cancellationToken);
        }

        public IQueryable<SchemaVersion> GetSchemaVersions()
        {
            return _schemaVersions.AsQueryable();
        }

        public Task<SchemaVersion> GetSchemaVersionAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return _schemaVersions.AsQueryable()
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<IReadOnlyDictionary<Guid, SchemaVersion>> GetSchemaVersionsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            var list = new List<Guid>(ids);

            List<SchemaVersion> result = await _schemaVersions.AsQueryable()
                .Where(t => list.Contains(t.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Id);
        }

        public Task AddSchemaVersionAsync(
            SchemaVersion schemaVersion,
            CancellationToken cancellationToken = default)
        {
            return _schemaVersions.InsertOneAsync(
                schemaVersion,
                options: null,
                cancellationToken);
        }

        public Task UpdateSchemaVersionAsync(
            SchemaVersion schemaVersion,
            CancellationToken cancellationToken = default)
        {
            return _schemaVersions.ReplaceOneAsync(
                Builders<SchemaVersion>.Filter.Eq(t => t.Id, schemaVersion.Id),
                schemaVersion,
                options: default(ReplaceOptions),
                cancellationToken);
        }
    }
}
