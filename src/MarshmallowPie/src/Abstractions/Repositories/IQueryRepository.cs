using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MarshmallowPie.Repositories
{
    public interface IQueryRepository
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

        Task<IReadOnlyDictionary<Guid, ClientVersion>> GetClientVersionsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default);

        Task AddClientVersionAsync(
            ClientVersion clientVersion,
            CancellationToken cancellationToken = default);

        Task UpdateSchemaVersionAsync(
            ClientVersion clientVersion,
            CancellationToken cancellationToken = default);

        Task<Query?> GetQueryAsync(
            string documentHash,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, Query>> GetQueriesAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default);

        Task AddQueryAsync(
            Query query,
            CancellationToken cancellationToken = default);

        IQueryable<ClientPublishReport> GetPublishReports(Guid? clientVersionId);

        Task<ClientPublishReport?> GetPublishReportAsync(
            Guid clientVersionId,
            Guid environmentId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, ClientPublishReport>> GetPublishReportsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default);

        Task AddPublishReportAsync(
            ClientPublishReport publishReport,
            CancellationToken cancellationToken = default);

        Task UpdatePublishReportAsync(
            ClientPublishReport publishReport,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, PublishedClient>> GetPublishedClientAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default);

        Task UpdatePublishedClientAsync(
            PublishedClient publishedClient,
            CancellationToken cancellationToken = default);
    }
}
