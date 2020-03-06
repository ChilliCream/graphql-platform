using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Clients
{
    public sealed class ClientByNameDataLoader
        : BatchDataLoader<string, Client>
    {
        private readonly IClientRepository _repository;

        public ClientByNameDataLoader(IClientRepository repository)
        {
            _repository = repository;
        }

        protected override Task<IReadOnlyDictionary<string, Client>> FetchBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken) =>
            _repository.GetClientsAsync(keys, cancellationToken);
    }
}
