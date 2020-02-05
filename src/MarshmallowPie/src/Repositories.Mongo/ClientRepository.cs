using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MarshmallowPie.Repositories.Mongo
{
    public class ClientRepository
        : IClientRepository
    {
        private readonly IMongoCollection<Client> _clients;
        private readonly IMongoCollection<ClientVersion> _versions;
        private readonly IMongoCollection<QueryDocument> _queries;
        private readonly IMongoCollection<ClientPublishReport> _publishReports;
        private readonly IMongoCollection<PublishedClient> _publishedClients;

        public ClientRepository(
            IMongoCollection<Client> clients,
            IMongoCollection<ClientVersion> versions,
            IMongoCollection<QueryDocument> queries,
            IMongoCollection<ClientPublishReport> publishReports,
            IMongoCollection<PublishedClient> publishedClients)
        {
            _clients = clients;
            _versions = versions;
            _queries = queries;
            _publishReports = publishReports;
            _publishedClients = publishedClients;

            _clients.Indexes.CreateOne(
                new CreateIndexModel<Client>(
                    Builders<Client>.IndexKeys.Ascending(x => x.Name),
                    new CreateIndexOptions { Unique = true }));

            _versions.Indexes.CreateOne(
                new CreateIndexModel<ClientVersion>(
                    Builders<ClientVersion>.IndexKeys.Ascending(x => x.ExternalId),
                    new CreateIndexOptions { Unique = true }));

            _queries.Indexes.CreateOne(
                new CreateIndexModel<QueryDocument>(
                    Builders<QueryDocument>.IndexKeys.Combine(
                        Builders<QueryDocument>.IndexKeys.Ascending(x => x.Hash.Hash),
                        Builders<QueryDocument>.IndexKeys.Ascending(x => x.SchemaId)),
                    new CreateIndexOptions { Unique = true }));

            _queries.Indexes.CreateOne(
                new CreateIndexModel<QueryDocument>(
                    Builders<QueryDocument>.IndexKeys.Ascending(
                        $"{nameof(QueryDocument.ExternalHashes)}.{nameof(DocumentHash.Hash)}"),
                    new CreateIndexOptions { Unique = false }));

            _publishReports.Indexes.CreateOne(
                new CreateIndexModel<ClientPublishReport>(
                    Builders<ClientPublishReport>.IndexKeys.Combine(
                        Builders<ClientPublishReport>.IndexKeys.Ascending(x =>
                            x.ClientVersionId),
                        Builders<ClientPublishReport>.IndexKeys.Ascending(x =>
                            x.EnvironmentId)),
                    new CreateIndexOptions { Unique = true }));

            _publishedClients.Indexes.CreateOne(
                new CreateIndexModel<PublishedClient>(
                    Builders<PublishedClient>.IndexKeys.Combine(
                        Builders<PublishedClient>.IndexKeys.Ascending(x => x.EnvironmentId),
                        Builders<PublishedClient>.IndexKeys.Ascending(x => x.ClientId)),
                    new CreateIndexOptions { Unique = true }));
        }

        public IQueryable<Client> GetClients(Guid? schemaId = null)
        {
            IQueryable<Client> clients = _clients.AsQueryable();

            if (schemaId.HasValue)
            {
                return clients.Where(t => t.SchemaId == schemaId.Value);
            }

            return clients;
        }

        public Task<Client> GetClientAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return _clients.AsQueryable()
                .Where(t => t.Id == id)
                .FirstAsync(cancellationToken);
        }

        public Task<Client?> GetClientAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            return _clients.AsQueryable()
                .Where(t => t.Name == name)
                .FirstOrDefaultAsync()!;
        }

        public async Task<IReadOnlyDictionary<Guid, Client>> GetClientsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            List<Client> result = await _clients.AsQueryable()
                .Where(t => ids.Contains(t.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Id);
        }

        public async Task<IReadOnlyDictionary<string, Client>> GetClientsAsync(
            IReadOnlyList<string> names,
            CancellationToken cancellationToken = default)
        {
            List<Client> result = await _clients.AsQueryable()
                .Where(t => names.Contains(t.Name))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Name);
        }

        public async Task AddClientAsync(
            Client client,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _clients.InsertOneAsync(
                    client,
                    options: null,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    $"The specified client name `{client.Name}` already exists.",
                    ex);
            }
        }

        public async Task UpdateClientAsync(
            Client client,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _clients.ReplaceOneAsync(
                    Builders<Client>.Filter.Eq(t => t.Id, client.Id),
                    client,
                    options: default(ReplaceOptions),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    $"The specified client name `{client.Name}` already exists.",
                    ex);
            }
        }

        public IQueryable<ClientVersion> GetClientVersions(Guid? clientId = null)
        {
            IQueryable<ClientVersion> clients = _versions.AsQueryable();

            if (clientId.HasValue)
            {
                return clients.Where(t => t.ClientId == clientId.Value);
            }

            return clients;
        }

        public async Task<ClientVersion?> GetClientVersionByExternalIdAsync(
            string externalId,
            CancellationToken cancellationToken = default)
        {
            return await _versions.AsQueryable()
                .Where(t => t.ExternalId == externalId)
                .FirstOrDefaultAsync(cancellationToken)!;
        }

        public async Task<IReadOnlyDictionary<Guid, ClientVersion>> GetClientVersionsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            List<ClientVersion> result = await _versions.AsQueryable()
                .Where(t => ids.Contains(t.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Id);
        }

        public async Task AddClientVersionAsync(
            ClientVersion clientVersion,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _versions.InsertOneAsync(
                    clientVersion,
                    options: null,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    $"The specified external ID `{clientVersion.Id}` is already used.",
                    ex);
            }
        }

        public async Task UpdateClientVersionTagsAsync(
            Guid clientVersionId,
            IReadOnlyList<Tag> tags,
            CancellationToken cancellationToken = default)
        {
            if (tags.Count > 1)
            {
                tags = new HashSet<Tag>(tags, TagComparer.Default).ToList();
            }

            await _versions.UpdateOneAsync(
                Builders<ClientVersion>.Filter.Eq(t => t.Id, clientVersionId),
                Builders<ClientVersion>.Update.Set(t => t.Tags, tags),
                options: default(UpdateOptions),
                cancellationToken)
                .ConfigureAwait(false);
        }

        public IQueryable<ClientPublishReport> GetPublishReports(Guid? clientVersionId)
        {
            IQueryable<ClientPublishReport> publishReports = _publishReports.AsQueryable();

            if (clientVersionId.HasValue)
            {
                return publishReports.Where(t => t.ClientVersionId == clientVersionId.Value);
            }

            return publishReports;
        }

        public Task<ClientPublishReport?> GetPublishReportAsync(
            Guid clientVersionId,
            Guid environmentId,
            CancellationToken cancellationToken = default)
        {
            return _publishReports.AsQueryable()
                .Where(t => t.ClientVersionId == clientVersionId
                    || t.EnvironmentId == environmentId)
                .FirstOrDefaultAsync()!;
        }

        public async Task<IReadOnlyDictionary<Guid, ClientPublishReport>> GetPublishReportsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            List<ClientPublishReport> result = await _publishReports.AsQueryable()
                .Where(t => ids.Contains(t.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Id);
        }

        public async Task AddPublishReportAsync(
            ClientPublishReport publishReport,
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
                    "TODO",
                    ex);
            }
        }

        public async Task UpdatePublishReportAsync(
            ClientPublishReport publishReport,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _publishReports.ReplaceOneAsync(
                    Builders<ClientPublishReport>.Filter.Eq(t => t.Id, publishReport.Id),
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
                    "TODO",
                    ex);
            }
        }

        public async Task<QueryDocument?> GetQueryDocumentAsync(
            Guid schemaId,
            string documentHash,
            CancellationToken cancellationToken = default)
        {
            QueryDocument? document = await _queries.AsQueryable()
                .Where(t => t.Hash.Hash == documentHash)
                .FirstOrDefaultAsync(cancellationToken)!
                .ConfigureAwait(false);

            if (document is null)
            {
                document = await _queries.AsQueryable()
                    .Where(t => t.ExternalHashes.Any(t => t.Hash == documentHash))
                    .FirstOrDefaultAsync(cancellationToken)!
                    .ConfigureAwait(false);
            }

            return document;
        }

        public async Task<QueryDocument?> GetQueryDocumentAsync(
            Guid environmentId,
            Guid schemaId,
            string documentHash,
            CancellationToken cancellationToken = default)
        {
            QueryDocument? document = await GetQueryDocumentAsync(
                schemaId, documentHash, cancellationToken)
                .ConfigureAwait(false);

            if (document is { })
            {
                IAsyncCursor<PublishedClient> cursor = await _publishedClients.FindAsync(
                    Builders<PublishedClient>.Filter.And(
                        Builders<PublishedClient>.Filter.Eq(t => t.EnvironmentId, environmentId),
                        Builders<PublishedClient>.Filter.Eq(t => t.SchemaId, schemaId),
                        Builders<PublishedClient>.Filter.AnyEq(t => t.QueryIds, document.Id)),
                    options: default,
                    cancellationToken)
                    .ConfigureAwait(false);

                if (await cursor.AnyAsync(cancellationToken).ConfigureAwait(false))
                {
                    return document;
                }
            }

            return null;
        }

        public async Task<IReadOnlyDictionary<Guid, QueryDocument>> GetQueryDocumentsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            List<QueryDocument> result = await _queries.AsQueryable()
                .Where(t => ids.Contains(t.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Id);
        }

        public async Task<IReadOnlyDictionary<string, QueryDocument>> GetQueryDocumentsAsync(
            Guid schemaId,
            IReadOnlyList<string> documentHashes,
            CancellationToken cancellationToken = default)
        {
            List<QueryDocument> result = await _queries.AsQueryable()
                .Where(t => documentHashes.Contains(t.Hash.Hash))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Hash.Hash);
        }

        public async Task AddQueryDocumentAsync(
            IEnumerable<QueryDocument> queries,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _queries.InsertManyAsync(
                    queries,
                    options: default(InsertManyOptions),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    "TODO.",
                    ex);
            }
        }

        public async Task<IReadOnlyDictionary<Guid, PublishedClient>> GetPublishedClientAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            List<PublishedClient> result = await _publishedClients.AsQueryable()
                .Where(t => ids.Contains(t.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Id);
        }

        public async Task<PublishedClient> GetPublishedClientAsync(
            Guid clientId,
            Guid environmentId,
            CancellationToken cancellationToken = default)
        {
            PublishedClient? client = await _publishedClients.AsQueryable()
                .Where(t => t.ClientId == clientId && t.EnvironmentId == environmentId)
                .FirstOrDefaultAsync(cancellationToken)!
                .ConfigureAwait(false);

            if (client is null)
            {
                throw new InvalidOperationException(
                    "There is no client version published with the client " +
                    $"id `{clientId}` to the environment `{environmentId}`.");
            }

            return client;
        }

        public async Task SetPublishedClientAsync(
            PublishedClient publishedClient,
            CancellationToken cancellationToken = default)
        {
            await _publishedClients.UpdateOneAsync(
                Builders<PublishedClient>.Filter.And(
                    Builders<PublishedClient>.Filter.Eq(
                        t => t.EnvironmentId,
                        publishedClient.EnvironmentId),
                    Builders<PublishedClient>.Filter.Eq(
                        t => t.SchemaId,
                        publishedClient.SchemaId)),
                Builders<PublishedClient>.Update.Combine(
                    Builders<PublishedClient>.Update.SetOnInsert(
                        t => t.EnvironmentId, publishedClient.EnvironmentId),
                    Builders<PublishedClient>.Update.SetOnInsert(
                        t => t.SchemaId, publishedClient.SchemaId),
                    Builders<PublishedClient>.Update.SetOnInsert(
                        t => t.ClientId, publishedClient.ClientId),
                    Builders<PublishedClient>.Update.Set(
                        t => t.ClientVersionId, publishedClient.ClientVersionId),
                    Builders<PublishedClient>.Update.Set(
                        t => t.QueryIds, publishedClient.QueryIds)),
                new UpdateOptions { IsUpsert = true },
                cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
