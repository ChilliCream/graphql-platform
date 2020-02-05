using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Clients
{
    public sealed class ClientByIdDataLoader
        : BatchDataLoader<Guid, Client>
    {
        private readonly IClientRepository _repository;

        public ClientByIdDataLoader(IClientRepository repository)
        {
            _repository = repository;
        }

        protected override Task<IReadOnlyDictionary<Guid, Client>> FetchBatchAsync(
            IReadOnlyList<Guid> keys,
            CancellationToken cancellationToken) =>
            _repository.GetClientsAsync(keys, cancellationToken);
    }

    public sealed class QueryDocumentByIdDataLoader
        : BatchDataLoader<Guid, QueryDocument>
    {
        private readonly IClientRepository _repository;

        public QueryDocumentByIdDataLoader(IClientRepository repository)
        {
            _repository = repository;
        }

        protected override Task<IReadOnlyDictionary<Guid, QueryDocument>> FetchBatchAsync(
            IReadOnlyList<Guid> keys,
            CancellationToken cancellationToken) =>
            _repository.GetQueryDocumentsAsync(keys, cancellationToken);
    }
}
