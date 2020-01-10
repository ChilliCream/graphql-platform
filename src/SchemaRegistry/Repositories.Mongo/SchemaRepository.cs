using System.Security.Cryptography;
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
        private readonly IMongoCollection<SchemaVersion> _versions;
        private readonly IMongoCollection<SchemaPublishReport> _publishReports;

        public SchemaRepository(
            IMongoCollection<Schema> schemas,
            IMongoCollection<SchemaVersion> versions,
            IMongoCollection<SchemaPublishReport> publishReports)
        {
            _schemas = schemas;
            _versions = versions;
            _publishReports = publishReports;

            _schemas.Indexes.CreateOne(
                new CreateIndexModel<Schema>(
                    Builders<Schema>.IndexKeys.Ascending(x => x.Name),
                    new CreateIndexOptions { Unique = true }));

            _versions.Indexes.CreateOne(
                new CreateIndexModel<SchemaVersion>(
                    Builders<SchemaVersion>.IndexKeys.Ascending(x => x.Hash),
                    new CreateIndexOptions { Unique = true }));

            _publishReports.Indexes.CreateOne(
                new CreateIndexModel<SchemaPublishReport>(
                    Builders<SchemaPublishReport>.IndexKeys.Combine(
                        Builders<SchemaPublishReport>.IndexKeys.Ascending(x =>
                            x.SchemaVersionId),
                        Builders<SchemaPublishReport>.IndexKeys.Ascending(x =>
                            x.EnvironmentId)),
                    new CreateIndexOptions { Unique = true }));
        }

        public IQueryable<Schema> GetSchemas() => _schemas.AsQueryable();

        public Task<Schema> GetSchemaAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return _schemas.AsQueryable()
                .Where(t => t.Id == id)
                .FirstAsync(cancellationToken);
        }

        public Task<Schema?> GetSchemaAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            return _schemas.AsQueryable()
                .Where(t => t.Name == name)
                .FirstOrDefaultAsync(cancellationToken)!;
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

        public async Task<IReadOnlyDictionary<string, Schema>> GetSchemasAsync(
            IReadOnlyList<string> names,
            CancellationToken cancellationToken = default)
        {
            var list = new List<string>(names);

            List<Schema> result = await _schemas.AsQueryable()
                .Where(t => list.Contains(t.Name))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Name);
        }

        public async Task AddSchemaAsync(
            Schema schema,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _schemas.InsertOneAsync(
                    schema,
                    options: null,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    $"The specified schema name `{schema.Name}` already exists.",
                    ex);
            }
        }

        public async Task UpdateSchemaAsync(
            Schema schema,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _schemas.ReplaceOneAsync(
                    Builders<Schema>.Filter.Eq(t => t.Id, schema.Id),
                    schema,
                    options: default(ReplaceOptions),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    $"The specified schema name `{schema.Name}` already exists.",
                    ex);
            }
        }

        public IQueryable<SchemaVersion> GetSchemaVersions()
        {
            return _versions.AsQueryable();
        }

        public Task<SchemaVersion?> GetSchemaVersionAsync(
            string hash,
            CancellationToken cancellationToken = default)
        {
            return _versions.AsQueryable()
                .Where(t => t.Hash == hash)
                .FirstOrDefaultAsync(cancellationToken)!;
        }

        public Task<SchemaVersion> GetSchemaVersionAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return _versions.AsQueryable()
                .Where(t => t.Id == id)
                .FirstAsync(cancellationToken);
        }

        public async Task<IReadOnlyDictionary<Guid, SchemaVersion>> GetSchemaVersionsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            var list = new List<Guid>(ids);

            List<SchemaVersion> result = await _versions.AsQueryable()
                .Where(t => list.Contains(t.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Id);
        }

        public async Task AddSchemaVersionAsync(
            SchemaVersion schemaVersion,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _versions.InsertOneAsync(
                    schemaVersion,
                    options: null,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    $"The specified schema version hash `{schemaVersion.Hash}` already exists.",
                    ex);
            }
        }

        public async Task UpdateSchemaVersionAsync(
            SchemaVersion schemaVersion,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _versions.ReplaceOneAsync(
                    Builders<SchemaVersion>.Filter.Eq(t => t.Id, schemaVersion.Id),
                    schemaVersion,
                    options: default(ReplaceOptions),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    $"The specified schema version hash `{schemaVersion.Hash}` already exists.",
                    ex);
            }
        }

        public IQueryable<SchemaPublishReport> GetPublishReports() =>
            _publishReports.AsQueryable();

        public Task<SchemaPublishReport?> GetPublishReportAsync(
            Guid schemaVersionId,
            Guid environmentId,
            CancellationToken cancellationToken = default)
        {
            return _publishReports.AsQueryable()
                .Where(t => t.SchemaVersionId == schemaVersionId
                    && t.EnvironmentId == environmentId)
                .FirstOrDefaultAsync(cancellationToken)!;
        }

        public async Task<IReadOnlyDictionary<Guid, SchemaPublishReport>> GetPublishReportsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            var list = new List<Guid>(ids);

            List<SchemaPublishReport> result = await _publishReports.AsQueryable()
                .Where(t => list.Contains(t.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Id);
        }

        public async Task AddPublishReportAsync(
            SchemaPublishReport publishReport,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _publishReports.InsertOneAsync(
                    publishReport,
                    options: null,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    "A schema publish report was already created for the specified " +
                    "schema version and environment.",
                    ex);
            }
        }

        public async Task UpdatePublishReportAsync(
            SchemaPublishReport publishReport,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _publishReports.ReplaceOneAsync(
                    Builders<SchemaPublishReport>.Filter.Eq(t => t.Id, publishReport.Id),
                    publishReport,
                    options: default(ReplaceOptions),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    "A schema publish report was already created for the specified " +
                    "schema version and environment.",
                    ex);
            }
        }
    }
}
