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
        private readonly IMongoCollection<Query> _queries;
        private readonly IMongoCollection<ClientPublishReport> _publishReports;

        public ClientRepository(
            IMongoCollection<Client> clients,
            IMongoCollection<ClientVersion> versions,
            IMongoCollection<Query> queries,
            IMongoCollection<ClientPublishReport> publishReports)
        {
            _clients = clients;
            _versions = versions;
            _queries = queries;
            _publishReports = publishReports;

            _clients.Indexes.CreateOne(
                new CreateIndexModel<Client>(
                    Builders<Client>.IndexKeys.Ascending(x => x.Name),
                    new CreateIndexOptions { Unique = true }));

            _versions.Indexes.CreateOne(
                new CreateIndexModel<ClientVersion>(
                    Builders<ClientVersion>.IndexKeys.Ascending(x => x.ExternalId),
                    new CreateIndexOptions { Unique = true }));

            _queries.Indexes.CreateOne(
                new CreateIndexModel<Query>(
                    Builders<Query>.IndexKeys.Ascending(x => x.Hash.Hash),
                    new CreateIndexOptions { Unique = false }));

            _queries.Indexes.CreateOne(
                new CreateIndexModel<Query>(
                    Builders<Query>.IndexKeys.Ascending(
                        $"{nameof(Query.ExternalHashes)}.{nameof(DocumentHash.Hash)}"),
                    new CreateIndexOptions { Unique = false }));

            _publishReports.Indexes.CreateOne(
                new CreateIndexModel<ClientPublishReport>(
                    Builders<ClientPublishReport>.IndexKeys.Combine(
                        Builders<ClientPublishReport>.IndexKeys.Ascending(x =>
                            x.ClientVersionId),
                        Builders<ClientPublishReport>.IndexKeys.Ascending(x =>
                            x.EnvironmentId)),
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
            var list = new List<Guid>(ids);

            List<Client> result = await _clients.AsQueryable()
                .Where(t => list.Contains(t.Id))
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

            if (schemaId.HasValue)
            {
                return clients.Where(t => t.SchemaId == schemaId.Value);
            }

            return clients;
        }

        public Task<IReadOnlyDictionary<Guid, ClientVersion>> GetClientVersionsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task AddClientVersionAsync(
            ClientVersion clientVersion,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdateClientVersionTagsAsync(
            Guid clientVersionId,
            IReadOnlyList<Tag> tags,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyDictionary<Guid, PublishedClient>> GetPublishedClientAsync(
                  IReadOnlyList<Guid> ids,
                  CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task AddPublishReportAsync(
            ClientPublishReport publishReport,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }


        public Task AddQueriesAsync(IEnumerable<Query> queries, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }





        public Task<ClientPublishReport?> GetPublishReportAsync(Guid clientVersionId, Guid environmentId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IQueryable<ClientPublishReport> GetPublishReports(Guid? clientVersionId)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyDictionary<Guid, ClientPublishReport>> GetPublishReportsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyDictionary<Guid, Query>> GetQueriesAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Query?> GetQueryAsync(string documentHash, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }



        public Task UpdatePublishedClientAsync(PublishedClient publishedClient, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UpdatePublishReportAsync(ClientPublishReport publishReport, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
