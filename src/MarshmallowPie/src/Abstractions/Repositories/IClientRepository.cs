using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Repositories
{
    public interface IClientRepository
    {
        IQueryable<Client> GetClients(Guid? schemaId = null);

        Task<Client> GetClientAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<Client?> GetClientAsync(
            string name,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, Client>> GetClientsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<string, Client>> GetClientsAsync(
            IReadOnlyList<string> names,
            CancellationToken cancellationToken = default);

        Task AddClientAsync(
            Client client,
            CancellationToken cancellationToken = default);

        Task UpdateClientAsync(
            Client client,
            CancellationToken cancellationToken = default);

        IQueryable<ClientVersion> GetClientVersions(Guid? clientId = null);

        Task<ClientVersion?> GetClientVersionByExternalIdAsync(
            string externalId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, ClientVersion>> GetClientVersionsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default);

        Task AddClientVersionAsync(
            ClientVersion clientVersion,
            CancellationToken cancellationToken = default);

        Task UpdateClientVersionTagsAsync(
            Guid clientVersionId,
            IReadOnlyList<Tag> tags,
            CancellationToken cancellationToken = default);

        Task<QueryDocument?> GetQueryDocumentAsync(
            Guid schemaId,
            string documentHash,
            CancellationToken cancellationToken = default);

        Task<QueryDocument?> GetQueryDocumentAsync(
            Guid environmentId,
            Guid schemaId,
            string documentHash,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, QueryDocument>> GetQueryDocumentsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<string, QueryDocument>> GetQueryDocumentsAsync(
            Guid schemaId,
            IReadOnlyList<string> documentHashes,
            CancellationToken cancellationToken = default);

        Task AddQueryDocumentAsync(
            IEnumerable<QueryDocument> queries,
            CancellationToken cancellationToken = default);

        IQueryable<ClientPublishReport> GetPublishReports(Guid? clientVersionId);

        Task<ClientPublishReport?> GetPublishReportAsync(
            Guid clientVersionId,
            Guid environmentId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, ClientPublishReport>> GetPublishReportsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default);

        Task SetPublishReportAsync(
            ClientPublishReport publishReport,
            CancellationToken cancellationToken = default);

        Task<PublishedClient> GetPublishedClientAsync(
            Guid clientId, Guid environmentId,
            CancellationToken cancellationToken = default);

        Task SetPublishedClientAsync(
            PublishedClient publishedClient,
            CancellationToken cancellationToken = default);
    }
}
